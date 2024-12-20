﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using WitherTorch.Core.Property;
using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    public sealed class CachedDownloadClient
    {
        private static readonly Lazy<CachedDownloadClient> _inst = new Lazy<CachedDownloadClient>(
            () => new CachedDownloadClient(), LazyThreadSafetyMode.PublicationOnly);

        private readonly HttpClient _client;
        private AutoDisposer<JsonPropertyFile>? _disposableCacheFile;
        private int _cacheFileRefresh;
        private CancellationTokenSource? _saveCacheFileToken;

        public static CachedDownloadClient Instance => _inst.Value;
        public static bool HasInstance => _inst.IsValueCreated;

        private CachedDownloadClient()
        {
            HttpClient client = new HttpClient();
            HttpRequestHeaders headers = client.DefaultRequestHeaders;
            headers.Add("User-Agent", Constants.UserAgent);
            headers.Accept.Add(MIMETypes.JSON);
            headers.Accept.Add(MIMETypes.XML);
            _client = client;
            _disposableCacheFile = AutoDisposer.Create(new JsonPropertyFile(Path.GetFullPath(Path.Combine(WTCore.CachePath, "./index.json")), true, true));
        }

        private void SaveCacheFile(Task? lastTask)
        {
            if (lastTask is not null && !lastTask.IsCompleted)
                return;
            JsonPropertyFile? cacheFile = Interlocked.CompareExchange(ref _disposableCacheFile, null, null)?.Data;
            if (cacheFile is null)
                return;
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

        internal void ResetCache()
        {
            Interlocked.Exchange(ref _cacheFileRefresh, 1);
        }

        public HttpClient InnerHttpClient => _client;

        public string? DownloadString(string address)
        {
            string tokenKey = address.Replace(".", "%2E");
            string? result = null, path = null;
            bool hasCacheFile = false;
            bool isCacheNotOutdated = false;

            AutoDisposer<JsonPropertyFile> disposableCacheFile;

            if (Interlocked.Exchange(ref _cacheFileRefresh, 0) != 0)
            {
                Interlocked.Exchange(ref _disposableCacheFile,
                    disposableCacheFile = ObjectUtils.ThrowIfNull(
                        AutoDisposer.Create(new JsonPropertyFile(
                            Path.GetFullPath(Path.Combine(WTCore.CachePath, "./index.json")), true, true)))
                    );
            }
            else
            {
                disposableCacheFile = ObjectUtils.ThrowIfNull(Interlocked.CompareExchange(ref _disposableCacheFile, null, null));
            }

            JsonPropertyFile cacheFile = disposableCacheFile.Data;

            if (cacheFile[tokenKey] is JsonObject tokenObject)
            {
                long? expiredTime = tokenObject["expiredTime"]?.GetValue<long>();
                if (expiredTime.HasValue)
                {
                    DateTime now = DateTime.UtcNow;
                    DateTime exTime = DateTime.FromBinary(expiredTime.Value);
                    isCacheNotOutdated = (now - exTime) <= WTCore.CacheFileTTL;
                }
                string? value = tokenObject["value"]?.GetValue<string>();
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
                tokenObject = new JsonObject();
                cacheFile[tokenKey] = tokenObject;
            }

            if (isCacheNotOutdated && hasCacheFile && path is not null)
            {
                try
                {
                    using StreamReader reader = new StreamReader(path);
                    result = reader.ReadToEnd();
                    reader.Close();
                }
                catch (IOException) when (result is null)
                {
                    hasCacheFile = false;
                }
                catch (Exception)
                {

                }
            }

            string? downloadedString = null;
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
#else
                    using Task<string> downloadTask = _client.GetStringAsync(address);
                    if (downloadTask.Wait(WTCore.CDCDownloadTimeout))
                        downloadedString = downloadTask.Result;
#endif
                }
                catch (Exception)
                {
                }
                if (downloadedString is null && hasCacheFile && path is not null)
                {
                    try
                    {
                        using StreamReader reader = new StreamReader(path);
                        result = reader.ReadToEnd();
                        reader.Close();
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
                        string? value = null;
                        bool hasKey = false;
                        if (tokenObject.TryGetPropertyValue("value", out JsonNode? valueToken))
                        {
                            value = valueToken?.GetValue<string>();
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
                    if (path is not null)
                    {
                        try
                        {
                            using StreamWriter writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);
                            writer.Write(downloadedString);
                            writer.Flush();
                            writer.Close();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    CancellationTokenSource saveCacheFileToken = new CancellationTokenSource();
                    CancellationTokenSource? saveCacheFileTokenOld = Interlocked.Exchange(ref _saveCacheFileToken, saveCacheFileToken);
                    if (saveCacheFileTokenOld is not null)
                    {
                        try
                        {
                            saveCacheFileTokenOld.Cancel();
                            saveCacheFileTokenOld.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (!saveCacheFileToken.IsCancellationRequested)
                        Task.Delay(1000, saveCacheFileToken.Token).ContinueWith(SaveCacheFile);
                }
            }

            return result;
        }

        ~CachedDownloadClient()
        {
            Dispose();
        }

        private void Dispose()
        {
            _client.Dispose();
            CancellationTokenSource? saveCacheFileTokenOld = Interlocked.Exchange(ref _saveCacheFileToken, null);
            if (saveCacheFileTokenOld is not null)
            {
                try
                {
                    saveCacheFileTokenOld.Cancel();
                    saveCacheFileTokenOld.Dispose();
                }
                catch (Exception)
                {
                }
            }
            SaveCacheFile(null);
        }
    }
}
