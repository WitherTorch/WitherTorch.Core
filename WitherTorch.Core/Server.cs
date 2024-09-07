using System;
using System.IO;
using Newtonsoft.Json.Linq;

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

        // 面向外部的空參數建構子
        public Server()
        {
        }

        public override string GetSoftwareID()
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

        public delegate void ServerInstallingEventHandler(object sender, InstallTask task);

        /// <summary>
        /// 當伺服器的名稱改變時觸發
        /// </summary>
        public event EventHandler ServerNameChanged;

        /// <summary>
        /// 當伺服器的版本改變時觸發
        /// </summary>
        public event EventHandler ServerVersionChanged;

        /// <summary>
        /// 當伺服器正在安裝軟體時觸發
        /// </summary>
        public event ServerInstallingEventHandler ServerInstalling;

        // 內部空參數建構子 (防止有第三方伺服器軟體類別繼承自它)
        internal Server()
        {
        }

        /// <summary>
        /// 檢測是否為指定類別的子伺服器類別
        /// </summary>
        /// <param name="baseServerType">欲查詢的基底伺服器類別</param>
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
        /// 伺服器版本
        /// </summary>
        /// <remarks>
        /// 若要保證取得可讀的伺服器版本，請使用 <see cref="GetReadableVersion"/> 函數
        /// </remarks>
        public abstract string ServerVersion { get; }

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
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server GetServerFromDirectory(string serverDirectory)
        {
            if (File.Exists(Path.Combine(serverDirectory, @"server_info.json")))
            {
                JsonPropertyFile serverInformation = new JsonPropertyFile(Path.Combine(serverDirectory, @"server_info.json"), true, true);
                string softwareID = serverInformation["software"]?.Value<string>();
                Type softwareType = SoftwareRegister.GetSoftwareTypeFromID(softwareID);
                if (softwareType is null)
                {
                    throw new ServerSoftwareIsNotRegisteredException(softwareID);
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
                Type softwareType = SoftwareRegister.GetSoftwareTypeFromID(softwareID);
                if (softwareType is null)
                {
                    throw new ServerSoftwareIsNotRegisteredException(softwareID);
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
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        public static T CreateServer<T>(string serverDirectory) where T : Server
        {
            return (T)CreateServerInternal(typeof(T), serverDirectory);
        }

        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <param name="softwareType">軟體的型別</typeparam>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        public static Server CreateServer(Type softwareType, string serverDirectory)
        {
            if (softwareType?.IsSubclassOf(typeof(Server)) == true)
                return CreateServerInternal(softwareType, serverDirectory);
            else
                return null;
        }

        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <param name="softwareID">軟體的ID</param>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        public static Server CreateServer(string softwareID, string serverDirectory)
        {
            return CreateServerInternal(SoftwareRegister.GetSoftwareTypeFromID(softwareID), serverDirectory);
        }

        internal static Server CreateServerInternal(Type softwareType, string serverDirectory)
        {
            if (SoftwareRegister.registeredServerSoftwares.ContainsKey(softwareType))
            {
                Server server = Activator.CreateInstance(softwareType) as Server;
                server.ServerDirectory = serverDirectory;
                server.ServerName = GetDefaultServerName(server);
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
                throw new ServerSoftwareIsNotRegisteredException(softwareType);
            }
            return null;
        }
        /// <summary>
        /// 取得預設的伺服器名稱 (通常是伺服器資料夾的名字)
        /// </summary>
        /// <returns>是否成功加載伺服器</returns>
        public static string GetDefaultServerName(Server server)
        {
            string serverDirectory = server.ServerDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if (serverDirectory.Length > 3)
            {
                return Path.GetFileName(serverDirectory);
            }
            return serverDirectory;
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
        public abstract string GetSoftwareID();
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

        /// <summary>
        /// 子類別應覆寫此方法為儲存伺服器的程式碼
        /// </summary>
        /// <returns>是否成功儲存伺服器</returns>
        protected abstract bool BeforeServerSaved();

        protected void OnServerInstalling(InstallTask task)
        {
            ServerInstalling?.Invoke(this, task);
        }
                
        protected void OnServerVersionChanged()
        {
            ServerVersionChanged?.Invoke(this, EventArgs.Empty);
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
            if (BeforeServerSaved())
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
                        propertyFile?.Save(false);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

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
