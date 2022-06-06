using System;

namespace WitherTorch.Core
{
    /// <summary>
    /// WitherTorch.Core 的基礎控制類別，此類別是靜態類別<br/>
    /// 此類別收錄各種基礎設定，如 快取資料的根資料夾 等
    /// </summary>
    public static class WTCore
    {
        private static string _cachePath = "./Cache";
        private static string _spigotBuildToolsPath = "./SpigotBuildTools";
        private static string _fabricInstallerPath = "./FabricInstaller";
        /// <summary>
        /// 快取資料的根位置
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
        /// Spigot BuildTools 的根位置
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
        /// Fabric Installer 的根位置
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
        /// 註冊伺服器軟體時的最大容許時間
        /// </summary>
        public static int RegisterSoftwareTimeout { get; set; } = System.Threading.Timeout.Infinite;

        /// <summary>
        /// 是否在設定檔類別 (xxxPropertyFile) 中使用延遲載入 (需要引用時才載入) 來節省初始記憶體
        /// </summary>
        public static bool UseLazyLoadingOnPropertyFiles { get; set; } = false;

        public static bool RedirectSystemProcessStream { get; set; } = true;
    }
}
