using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WitherTorch.Core
{
    public class CachedDownloadClient : IDisposable
    {
        private static volatile JsonPropertyFile cacheFile;
        private static CachedDownloadClient _inst;
        private System.Net.Http.HttpClient _client;
        private CancellationTokenSource cacheSavingTaskToken;
        private bool disposedValue;

        public static CachedDownloadClient Instance
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_inst is null || _inst.disposedValue)
                {
                    _inst = new CachedDownloadClient();
                }
                return _inst;
            }
        }

        private static void SaveCacheFile(Task lastTask)
        {
            if (lastTask?.IsCompleted != false)
            {
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
        }

        private CachedDownloadClient()
        {
            _client = new System.Net.Http.HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", Constants.UserAgent);
            _client.DefaultRequestHeaders.Accept.Add(Utils.MIMETypes.JSON);
            _client.DefaultRequestHeaders.Accept.Add(Utils.MIMETypes.XML);
            ResetCache();
        }

        internal static void ResetCache()
        {
            cacheFile?.Dispose();
            cacheFile = new JsonPropertyFile(Path.GetFullPath(Path.Combine(WTCore.CachePath, "./index.json")), true, true);
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
                    isCacheNotOutdated = (now - exTime) <= WTCore.CacheFileTTL;
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
                catch (IOException) when (result is null)
                {
                    hasCacheFile = false;
                }
                catch (Exception)
                {

                }
            }

            string downloadedString = null;
            if (result is null)
            {
                try
                {
#if NET5_0_OR_GREATER
                    using CancellationTokenSource downloadTokenSource = new CancellationTokenSource();
                    using Task<string> downloadTask = _client.GetStringAsync(address);
                    if (downloadTask.Wait(WTCore.CDCDownloadTimeout))
                        downloadedString = downloadTask.Result;
                    else
                        downloadTokenSource.Cancel(true);
#elif NET472_OR_GREATER
                    using (Task<string> downloadTask = _client.GetStringAsync(address))
                    {
                        if (downloadTask.Wait(WTCore.CDCDownloadTimeout))
                            downloadedString = downloadTask.Result;
                    }
#endif
                }
                catch (Exception)
                {
                }
                if (downloadedString is null && hasCacheFile)
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
                    tokenObject["expiredTime"] = DateTime.UtcNow.ToBinary();
                    if (!hasCacheFile)
                    {
                        string value = null;
                        bool hasKey = false;
                        if (tokenObject.TryGetValue("value", out JToken valueToken))
                        {
                            value = valueToken?.Value<string>();
                            hasKey = true;
                        }
                        if (value is null)
                        {
                            value = Guid.NewGuid().ToString("N");
                            if (hasKey)
                                tokenObject["value"] = value;
                            else
                                tokenObject.Add("value", value);
                        }
                        path = Path.Combine(WTCore.CachePath, "./" + value);
                    }
                    cacheFile[tokenKey] = tokenObject;
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
                    lock (this)
                    {
                        if (cacheSavingTaskToken is object)
                        {
                            cacheSavingTaskToken.Cancel();
                            cacheSavingTaskToken.Dispose();
                        }
                    }
                    cacheSavingTaskToken = new CancellationTokenSource();
                    Task.Delay(1000, cacheSavingTaskToken.Token).ContinueWith(SaveCacheFile);
                }
            }

            return result;
        }

        ~CachedDownloadClient()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }
                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                _client.Dispose();
                lock (this)
                {
                    if (cacheSavingTaskToken is object)
                    {
                        cacheSavingTaskToken.Cancel();
                        cacheSavingTaskToken.Dispose();
                    }
                }
                SaveCacheFile(null);
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
