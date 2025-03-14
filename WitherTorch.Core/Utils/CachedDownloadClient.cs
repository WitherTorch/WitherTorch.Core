using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
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
        public string? DownloadString(Uri address) => DownloadStringAsync(address).Result;

        /// <summary>
        /// 從指定的 <see cref="Uri"/> 中下載並傳回字串
        /// </summary>
        /// <param name="address">要下載的網址</param>
        /// <returns></returns>
        public string? DownloadString(string address) => DownloadStringAsync(address).Result;

        /// <summary>
        /// 從指定的 <see cref="Uri"/> 中下載並傳回字串
        /// </summary>
        /// <param name="address">要下載的網址</param>
        /// <returns></returns>
        public Task<string?> DownloadStringAsync(Uri address) => DownloadStringAsync(address.AbsoluteUri);

        /// <summary>
        /// 從指定的 <see langword="string"/> 中下載並傳回字串
        /// </summary>
        /// <param name="address">要下載的網址</param>
        /// <returns></returns>
        public async Task<string?> DownloadStringAsync(string address)
        {
            TimeSpan timeout = WTCore.CDCDownloadTimeout;
            if (timeout == Timeout.InfiniteTimeSpan)
                return await _storage.GetStorageDataOrTryRenewAsync(address, GetStringCoreAsync);
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            Task<string?> workerTask = _storage.GetStorageDataOrTryRenewAsync(address, GetStringCoreAsync);
            Task<string?> timeoutTask = TaskHelper.DelayWithResult<string?>(timeout);
            Task<string?> finishedTask = await Task.WhenAny(workerTask, timeoutTask);
            if (ReferenceEquals(finishedTask, timeoutTask))
            {
                try
                {
                    tokenSource.Cancel();
                }
                catch (Exception)
                {
                }
            }
            return finishedTask.Result;
        }

        private async Task<string?> GetStringCoreAsync(string address, CancellationToken token)
        {
#if NETSTANDARD2_0_OR_GREATER
            return await _client.GetStringAsync(address);
#else
            return await _client.GetStringAsync(address, token);
#endif
        }
    }
}
