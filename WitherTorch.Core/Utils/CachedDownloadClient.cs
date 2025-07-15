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
        public string? DownloadString(Uri address) => DownloadString(address.AbsoluteUri);

        /// <summary>
        /// 從指定的 <see cref="Uri"/> 中下載並傳回字串
        /// </summary>
        /// <param name="address">要下載的網址</param>
        /// <returns></returns>
        public string? DownloadString(string address)
        {
            TimeSpan timeout = WTCore.CDCDownloadTimeout;
            Task<string?> workingTask = _storage.GetStorageDataOrTryRenewAsync(address, GetStringCoreAsync);
            if (workingTask.Wait(timeout))
                return workingTask.Result;
            return null;
        }

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
        public Task<string?> DownloadStringAsync(string address)
        {
            TimeSpan timeout = WTCore.CDCDownloadTimeout;
            if (timeout == Timeout.InfiniteTimeSpan)
                return _storage.GetStorageDataOrTryRenewAsync(address, GetStringCoreAsync);
            return TaskHelper.WaitForResultAsync(
                factoryFunc: (token) => _storage.GetStorageDataOrTryRenewAsync(address, GetStringCoreAsync, token),
                delay: timeout);
        }

        private async Task<string?> GetStringCoreAsync(string address, CancellationToken token)
        {
            using HttpResponseMessage message = await _client.GetAsync(Uri.UnescapeDataString(address), HttpCompletionOption.ResponseContentRead, token);
            if (!message.IsSuccessStatusCode || token.IsCancellationRequested)
                return null;
#if NET8_0_OR_GREATER
            return await message.Content.ReadAsStringAsync(token);
#else
            return await message.Content.ReadAsStringAsync();
#endif
        }
    }
}
