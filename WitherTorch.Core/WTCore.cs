using System;

namespace WitherTorch.Core
{
    /// <summary>
    /// <b>WitherTorch.Core</b> 的基礎控制類別，此類別是靜態類別<br/>
    /// 此類別收錄各種基礎設定，如 快取資料的根資料夾 等
    /// </summary>
    public static class WTCore
    {
        private static string _cachePath = "./Cache";
        private static string _spigotBuildToolsPath = "./SpigotBuildTools";
        private static string _fabricInstallerPath = "./FabricInstaller";

        /// <summary>
        /// 取得或設定 <b>CacheDownloadClient</b> 產生的快取檔案所存放的位置
        /// </summary>
        public static string CachePath
        {
            get
            {
                try
                {
                    if (!System.IO.Directory.Exists(_cachePath))
                    {
                        System.IO.Directory.CreateDirectory(_cachePath);
                    }
                }
                catch (Exception)
                {
                }
                return _cachePath;
            }
            set
            {
                _cachePath = value;
                try
                {
                    if (!System.IO.Directory.Exists(_cachePath))
                    {
                        System.IO.Directory.CreateDirectory(_cachePath);
                    }
                    CachedDownloadClient.ResetCache();
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// <b>Spigot BuildTools</b> 的根位置
        /// </summary>
        public static string SpigotBuildToolsPath
        {
            get
            {
                return _spigotBuildToolsPath;
            }
            set
            {
                _spigotBuildToolsPath = value;
                try
                {
                    if (!System.IO.Directory.Exists(_spigotBuildToolsPath))
                    {
                        System.IO.Directory.CreateDirectory(_spigotBuildToolsPath);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// <b>Fabric Installer</b> 的根位置
        /// </summary>
        public static string FabricInstallerPath
        {
            get
            {
                return _fabricInstallerPath;
            }
            set
            {
                _fabricInstallerPath = value;
                try
                {
                    if (!System.IO.Directory.Exists(_fabricInstallerPath))
                    {
                        System.IO.Directory.CreateDirectory(_fabricInstallerPath);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 註冊伺服器軟體時的最大容許時間 (預設為無限等待)
        /// </summary>
        public static TimeSpan RegisterSoftwareTimeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;

        /// <summary>
        /// 取得或設定 <b>CacheDownloadClient</b> 所產生的快取檔案有效時間 (預設為一小時)
        /// </summary>
        public static TimeSpan CacheFileTTL { get; set; } = new TimeSpan(1, 0, 0);

        /// <summary>
        /// 是否在設定檔類別 (xxxPropertyFile) 中使用延遲載入 (需要引用時才載入) 來節省初始記憶體 (預設為否)
        /// </summary>
        public static bool UseLazyLoadingOnPropertyFiles { get; set; } = false;

        /// <summary>
        /// 是否重新導向 <b>SystemProcess</b> 內處理序的訊息流 (預設為是)
        /// </summary>
        public static bool RedirectSystemProcessStream { get; set; } = true;

        /// <summary>
        /// 是否檢查在下載後檢查伺服器檔案的雜湊碼 (如果該伺服器軟體類別有支援該功能的話)
        /// </summary>
        public static bool CheckFileHashIfExist { get; set; } = true;
    }
}
