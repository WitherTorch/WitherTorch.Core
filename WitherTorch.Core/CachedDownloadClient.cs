using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core
{
    public class CachedDownloadClient
    {
        internal const string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36";
        private static volatile JsonPropertyFile cacheFile;
        public static CachedDownloadClient Instance { get; private set; } = new CachedDownloadClient();
        volatile System.Net.Http.HttpClient _client;
        volatile static Task cacheSavingTask;
        volatile static CancellationTokenSource cacheSavingTaskToken;
        private static void SaveCacheFile()
        {
            Thread.Sleep(1000);
            lock (cacheFile)
            {
                try
                {
                    cacheFile.Save(false);
                }
                catch (Exception)
                {
                }
            }
        }

        private CachedDownloadClient()
        {
            _client = new System.Net.Http.HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            _client.DefaultRequestHeaders.Accept.Add(Utils.MIMETypes.JSON);
            _client.DefaultRequestHeaders.Accept.Add(Utils.MIMETypes.XML);
            ResetCache();
        }

        internal static void ResetCache()
        {
            cacheFile?.Dispose();
            cacheFile = new JsonPropertyFile(System.IO.Path.Combine(WTCore.CachePath, "./index.json"), true, true);
        }

        public System.Net.Http.HttpClient InnerHttpClient => _client;

        public string DownloadString(string address)
        {
            string tokenKey = address.Replace(".", "%2E");
            string result = null, path = null;
            bool hasCacheFile = false;
            bool isCacheNotOutdated = false;
            if (cacheFile[tokenKey] is JObject tokenObject)
            {
                long? expiredTime = tokenObject["expiredTime"]?.Value<long>();
                if (expiredTime.HasValue)
                {
                    DateTime now = DateTime.UtcNow;
                    DateTime exTime = DateTime.FromBinary(expiredTime.Value);
                    isCacheNotOutdated = (now - exTime).TotalMinutes < 60;
                }
                string value = tokenObject["value"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    path = Path.Combine(WTCore.CachePath, "./" + value);
                    if (File.Exists(path))
                    {
                        hasCacheFile = true;
                    }
                }
            }
            else
            {
                tokenObject = new JObject();
                cacheFile[tokenKey] = tokenObject;
            }

            if (isCacheNotOutdated && hasCacheFile)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        result = reader.ReadToEnd();
                        reader.Close();
                    }
                }
                catch (IOException) when (result == null)
                {
                    hasCacheFile = false;
                }
                catch (Exception)
                {

                }
            }

            string downloadedString = null;
            if (result == null)
            {
                try
                {
                    downloadedString = _client.GetStringAsync(address).Result;
                }
                catch (Exception)
                {
                }

                if (downloadedString == null && hasCacheFile)
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(path))
                        {
                            result = reader.ReadToEnd();
                            reader.Close();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    result = downloadedString;
                }
            }

            if (result != null)
            {
                if (!isCacheNotOutdated && downloadedString != null)
                {
                    if (tokenObject.ContainsKey("expiredTime"))
                    {
                        tokenObject["expiredTime"] = DateTime.UtcNow.ToBinary();
                    }
                    else
                    {
                        tokenObject.Add("expiredTime", DateTime.UtcNow.ToBinary());
                    }
                    if (!hasCacheFile)
                    {
                        string value = null;
                        bool hasKey = false;
                        if (tokenObject.TryGetValue("value", out JToken valueToken))
                        {
                            value = valueToken?.Value<string>();
                            hasKey = true;
                        }
                        if (value == null)
                        {
                            value = Guid.NewGuid().ToString("N");
                            if (hasKey)
                                tokenObject["value"] = value;
                            else
                                tokenObject.Add("value", value);
                        }
                        path = Path.Combine(WTCore.CachePath, "./" + value);
                    }
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(path, false))
                        {
                            writer.Write(downloadedString);
                            writer.Flush();
                            writer.Close();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    if (cacheSavingTask != null && !cacheSavingTask.IsCompleted)
                    {
                        cacheSavingTaskToken.Cancel();
                        cacheSavingTaskToken.Dispose();
                    }
                    cacheSavingTaskToken = new CancellationTokenSource();
                    cacheSavingTask = Task.Run(SaveCacheFile, cacheSavingTaskToken.Token);
                }
            }

            return result;
        }
    }
}
