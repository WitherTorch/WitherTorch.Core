using System;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// <b>WitherTorch.Core</b> 的基礎控制類別，此類別是靜態類別<br/>
    /// 此類別收錄各種基礎設定，如 快取資料的根資料夾 等
    /// </summary>
    public static class WTCore
    {
        /// <summary>
        /// 取得或設定 <see cref="CachedDownloadClient"/> 產生的快取檔案所存放的位置
        /// </summary>
        public static string CachePath { get; set; } = "./Cache";

        /// <summary>
        /// 註冊伺服器軟體時的最大容許時間 (預設為無限等待)
        /// </summary>
        public static TimeSpan RegisterSoftwareTimeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;

        /// <summary>
        /// 取得或設定 <see cref="CachedDownloadClient"/> 所產生的快取檔案有效時間 (預設為一小時)
        /// </summary>
        public static TimeSpan CacheFileTTL { get; set; } = new TimeSpan(1, 0, 0);

        /// <summary>
        /// 取得或設定 <see cref="CachedDownloadClient"/> 下載檔案時所允許的最長時間 (預設為15秒)
        /// </summary>
        public static TimeSpan CDCDownloadTimeout { get; set; } = new TimeSpan(0, 0, 15);

        /// <summary>
        /// 是否重新導向 <see cref="SystemProcess"/> 內處理序的訊息流 (預設為是)
        /// </summary>
        public static bool RedirectSystemProcessStream { get; set; } = true;

        /// <summary>
        /// 是否檢查在下載後檢查伺服器檔案的雜湊碼 (如果該伺服器軟體類別有支援該功能的話)
        /// </summary>
        public static bool CheckFileHashIfExist { get; set; } = true; 
        
        /// <summary>
        /// 是否在設定檔案受到外部更改時，自動重新載入檔案 (該操作為延遲載入，僅在該設定檔未受到任何未儲存的內部更改的情況下生效)
        /// </summary>
        public static bool WatchPropertyFileModified { get; set; } = true;
    }
}
