using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    public sealed class CachedDownloadClient
    {
        private static readonly CachedDownloadClient _instance = new CachedDownloadClient();

        private readonly CacheStorage _storage;
        private readonly HttpClient _client;

        public static CachedDownloadClient Instance => _instance;

        public HttpClient InnerHttpClient => _client;

        private CachedDownloadClient()
        {
            string dirPath = WTCore.CachePath;
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            _storage = new CacheStorage(Path.Combine(dirPath, "./cache_manifest.json"));
            _client = new HttpClient();
        }

        public string? DownloadString(Uri address)
        {
            return _storage.GetStorageDataOrTryRenew(address, () =>
            {
                HttpClient client = _client;
                TimeSpan timeout = WTCore.CDCDownloadTimeout;

#if NET5_0_OR_GREATER
                using CancellationTokenSource tokenSource = new CancellationTokenSource();
                using Task<string> task = client.GetStringAsync(address, tokenSource.Token);

                if (!task.Wait(timeout))
                {
                    tokenSource.Cancel();
                    return null;
                }

                return task.IsCompletedSuccessfully ? task.Result : null;
#else
                using Task<string> task = client.GetStringAsync(address);

                if (!task.Wait(timeout))
                    return null;

                return task.IsCompleted ? task.Result : null;
#endif
            });
        }

        public string? DownloadString(string address)
        {
            return _storage.GetStorageDataOrTryRenew(address, () =>
            {
                HttpClient client = _client;
                TimeSpan timeout = WTCore.CDCDownloadTimeout;

#if NET5_0_OR_GREATER
                using CancellationTokenSource tokenSource = new CancellationTokenSource();
                using Task<string> task = client.GetStringAsync(address, tokenSource.Token);

                if (!task.Wait(timeout))
                {
                    tokenSource.Cancel();
                    return null;
                }

                return task.IsCompletedSuccessfully ? task.Result : null;
#else
                using Task<string> task = client.GetStringAsync(address);

                if (!task.Wait(timeout))
                    return null;

                return task.IsCompleted ? task.Result : null;
#endif
            });
        }
    }
}
