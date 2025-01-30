using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using WitherTorch.Core.Utils;

#if NET5_0_OR_GREATER
using System.Threading;
#endif

namespace WitherTorch.Core
{
    /// <summary>
    /// 具有快取特性的下載用類別，此類別無法建立實例
    /// </summary>
    public sealed class CachedDownloadClient
    {
        private static readonly CachedDownloadClient _instance = new CachedDownloadClient();

        private readonly CacheStorage _storage;
        private readonly HttpClient _client;

        /// <summary>
        /// 取得 <see cref="CachedDownloadClient"/> 的唯一實例
        /// </summary>
        public static CachedDownloadClient Instance => _instance;

        /// <summary>
        /// 取得內部所使用的 <see cref="HttpClient"/> 物件
        /// </summary>
        public HttpClient InnerHttpClient => _client;

        private CachedDownloadClient()
        {
            string dirPath = WTCore.CachePath;
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            _storage = new CacheStorage(Path.Combine(dirPath, "./manifest.json"));
            _client = new HttpClient();
        }

        /// <summary>
        /// 從指定的 <see cref="Uri"/> 中下載並傳回字串
        /// </summary>
        /// <param name="address">要下載的網址</param>
        /// <returns></returns>
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

        /// <summary>
        /// 從指定的 <see langword="string"/> 中下載並傳回字串
        /// </summary>
        /// <param name="address">要下載的網址</param>
        /// <returns></returns>
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

                return (task.IsCompleted && task.Exception is null) ? task.Result : null;
#endif
            });
        }
    }
}
