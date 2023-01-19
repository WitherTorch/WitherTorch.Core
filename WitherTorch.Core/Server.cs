using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個伺服器，這個類別是虛擬泛型類別
    /// </summary>
    public abstract class Server<T> : Server where T : Server<T>
    {
        internal protected static Action SoftwareRegistrationDelegate { get; protected set; }
        /// <summary>
        /// 伺服器軟體ID
        /// </summary>
        internal protected static string SoftwareID { get; protected set; }

        internal static bool isNeedInitialize = true;

        /// <summary>
        /// 指示是否為伺服器軟體註冊時的初始化實體 <br/> <br/>
        /// 如果為 <c>true</c>, 軟體類別需初始化 <c>SoftwareRegistrationDelegate</c> 和 <c>SoftwareID</c> 欄位<br/>
        /// 如果為 <c>false</c>, 軟體類別應將此物件當作一般伺服器實體對待
        /// </summary>
        protected readonly bool IsInit;

        // 面向外部的空參數建構子
        public Server()
        {
            if (isNeedInitialize)
            {
                isNeedInitialize = false;
                IsInit = true;
            }
            else
            {
                IsInit = false;
            }
        }

        internal override string GetSoftwareID()
        {
            return SoftwareID;
        }
    }
    /// <summary>
    /// 表示一個伺服器，這個類別是虛擬類別
    /// </summary>
    public abstract class Server : IDisposable
    {
        private string _name;
        private bool disposedValue;

        public event EventHandler ServerNameChanged;

        // 內部空參數建構子 (防止有第三方伺服器軟體類別繼承自它)
        internal Server()
        {
        }

        /// <summary>
        /// 檢測是否為指定類別的子伺服器類別
        /// </summary>
        /// <param name="baseServerType">欲查詢的基底伺服器類別</param>
        /// <returns></returns>
        public bool IsSubclassOf(Type baseServerType)
        {
            Type TServer = GetType();
            return TServer.IsSubclassOf(baseServerType.MakeGenericType(TServer));
        }

        /// <summary>
        /// 檢測是否為指定類別的子伺服器類別
        /// </summary>
        /// <typeparam name="TServerBase">欲查詢的基底伺服器類別</typeparam>
        public bool IsSubclassOf<TServerBase>()
        {
            return IsSubclassOf(typeof(TServerBase));
        }

        /// <summary>
        /// 伺服器名稱
        /// </summary>
        public string ServerName
        {
            get { return _name; }
            set
            {
                _name = value;
                ServerNameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// 伺服器資料夾路徑
        /// </summary>
        public string ServerDirectory { get; set; }
        /// <summary>
        /// 取得人類可讀(human-readable)的軟體版本
        /// </summary>
        public abstract string GetReadableVersion();
        /// <summary>
        /// 取得伺服器軟體所有的可用版本
        /// </summary>
        public abstract string[] GetSoftwareVersions();
        /// <summary>
        /// 取得伺服器的設定檔案
        /// </summary>
        /// <returns></returns>
        public abstract IPropertyFile[] GetServerPropertyFiles();
        /// <summary>
        /// 取得伺服器的 server_info.json (伺服器基礎資訊清單)
        /// </summary>
        /// <returns></returns>
        public JsonPropertyFile ServerInfoJson { get; private set; }
        /// <summary>
        /// 取得伺服器的執行環境資訊
        /// </summary>
        /// <returns>若無特殊的執行環境資訊，應回傳 <see langword="null"/> 來指示伺服器軟體執行者以預設環境執行</returns>
        public abstract RuntimeEnvironment GetRuntimeEnvironment();
        /// <summary>
        /// 設定伺服器的執行環境資訊
        /// </summary>
        /// <returns></returns>
        public abstract void SetRuntimeEnvironment(RuntimeEnvironment environment);
        /// <summary>
        /// 取得伺服器
        /// </summary>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server GetServerFromDirectory(string serverDirectory)
        {
            if (File.Exists(Path.Combine(serverDirectory, @"server_info.json")))
            {
                JsonPropertyFile serverInformation = new JsonPropertyFile(Path.Combine(serverDirectory, @"server_info.json"), true, true);
                string softwareID = serverInformation["software"]?.Value<string>();
                Type softwareType = SoftwareRegister.GetSoftwareFromID(softwareID);
                if (softwareType is null)
                {
                    throw new ServerSoftwareIsNotRegisteredException();
                }
                else
                {
                    Server server = (Server)Activator.CreateInstance(softwareType);
                    server.ServerInfoJson = serverInformation;
                    server.ServerDirectory = Path.GetFullPath(serverDirectory);
                    server.ServerName = serverInformation["name"]?.Value<string>();
                    if (server.OnServerLoading())
                    {
                        return server;
                    }
                    else
                    {
                        try
                        {
                            server.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 取得伺服器
        /// </summary>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <param name="software">伺服器軟體 ID</param>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server GetServerFromDirectory(string serverDirectory, string softwareID)
        {
            if (File.Exists(Path.Combine(serverDirectory, @"server_info.json")))
            {
                JsonPropertyFile serverInformation = new JsonPropertyFile(Path.Combine(serverDirectory, @"server_info.json"), true, true);
                Type softwareType = SoftwareRegister.GetSoftwareFromID(softwareID);
                if (softwareType is null)
                {
                    throw new ServerSoftwareIsNotRegisteredException();
                }
                else
                {
                    Server server = (Server)Activator.CreateInstance(softwareType);
                    server.ServerInfoJson = serverInformation;
                    server.ServerDirectory = serverDirectory;
                    server.ServerName = serverInformation["name"]?.Value<string>();
                    if (server.OnServerLoading())
                    {
                        return server;
                    }
                    else
                    {
                        try
                        {
                            server.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <typeparam name="T">軟體的型別</typeparam>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        public static T CreateServer<T>(string serverDirectory) where T : Server
        {
            return CreateServerInternal(typeof(T), serverDirectory) as T;
        }

        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <param name="softwareType">軟體的型別</typeparam>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        public static Server CreateServer(Type softwareType, string serverDirectory)
        {
            if (softwareType?.IsSubclassOf(typeof(Server)) == true)
                return CreateServerInternal(softwareType, serverDirectory);
            else
                return null;
        }

        [Obsolete]
        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <param name="softwareID">軟體的ID</typeparam>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        public static Server CreateServer(string softwareID, string serverDirectory)
        {
            return CreateServerInternal(SoftwareRegister.GetSoftwareFromID(softwareID), serverDirectory);
        }

        internal static Server CreateServerInternal(Type softwareType, string serverDirectory)
        {
            if (SoftwareRegister.registeredServerSoftwares.ContainsKey(softwareType))
            {
                Server server = Activator.CreateInstance(softwareType) as Server;
                server.ServerDirectory = serverDirectory;
                if (server.CreateServer())
                {
                    return server;
                }
                else
                {
                    try
                    {
                        server.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                throw new ServerSoftwareIsNotRegisteredException();
            }
            return null;
        }
        /// <summary>
        /// 子類別應覆寫此方法為加載伺服器的程式碼
        /// </summary>
        /// <returns>是否成功加載伺服器</returns>
        protected abstract bool OnServerLoading();
        /// <summary>
        /// 子類別應覆寫此方法為建立伺服器的程式碼
        /// </summary>
        /// <returns>是否成功建立伺服器</returns>
        protected abstract bool CreateServer();
        /// <summary>
        /// 取得伺服器軟體ID
        /// </summary>
        internal abstract string GetSoftwareID();
        /// <summary>
        /// 更改伺服器軟體版本
        /// </summary>
        /// <param name="versionIndex">軟體傳回的版本索引值</param>
        /// <returns>是否成功更改伺服器軟體版本</returns>
        public abstract bool ChangeVersion(int versionIndex);
        /// <summary>
        /// 取得當前的處理序物件
        /// </summary>
        public abstract AbstractProcess GetProcess();
        /// <summary>
        /// 啟動伺服器
        /// </summary>
        public abstract void RunServer(RuntimeEnvironment environment);
        /// <summary>
        /// 停止伺服器
        /// </summary>
        public abstract void StopServer(bool force);
        /// <summary>
        /// 子類別應覆寫此方法為更新伺服器軟體的程式碼
        /// </summary>
        /// <returns>是否成功開始更新伺服器軟體</returns>
        public abstract bool UpdateServer();
        public delegate void ServerInstallingEventHandler(InstallTask task);
        public event ServerInstallingEventHandler ServerInstalling;
        protected void OnInstallSoftware(InstallTask task)
        {
            ServerInstalling?.Invoke(task);
        }
        /// <summary>
        /// 儲存伺服器
        /// </summary>
        public void SaveServer()
        {
            string configuationPath = Path.Combine(ServerDirectory, @"server_info.json");
            if (ServerInfoJson is null)
                ServerInfoJson = new JsonPropertyFile(configuationPath, true, true);
            ServerInfoJson["name"] = new JValue(ServerName);
            ServerInfoJson["software"] = new JValue(GetSoftwareID());
            if (OnServerSaving())
            {
                ServerInfoJson.Save(false);
            }
            try
            {
                IPropertyFile[] properties = GetServerPropertyFiles();
                if (properties != null)
                {
                    foreach (IPropertyFile propertyFile in properties)
                    {
                        propertyFile.Save(false);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// 子類別應覆寫此方法為儲存伺服器的程式碼
        /// </summary>
        /// <returns>是否成功儲存伺服器</returns>
        protected abstract bool OnServerSaving();

        /// <summary>
        /// 伺服器物件的標籤，可供操作者儲存額外的伺服器資訊<br/>
        /// 伺服器軟體本身不應使用該屬性。
        /// </summary>
        public object Tag { get; set; }

        #region Disposing
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        // ~Server()
        // {
        //     // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
        //     Dispose(disposing: false);
        // }

        ///<inheritdoc/>
        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
