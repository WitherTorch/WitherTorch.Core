using System;
using System.IO;
using System.Text.Json.Nodes;

using WitherTorch.Core.Property;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個伺服器，這個類別是虛擬泛型類別
    /// </summary>
#pragma warning disable CS8618
    public abstract class Server<T> : Server where T : Server<T>
    {
        internal protected static Action SoftwareRegistrationDelegate { get; protected set; }
        /// <summary>
        /// 伺服器軟體ID
        /// </summary>
        internal protected static string SoftwareId { get; protected set; }

        // 面向外部的空參數建構子
        public Server()
        {
        }

        public override string GetSoftwareId()
        {
            return SoftwareId;
        }
    }
#pragma warning restore CS8618

    /// <summary>
    /// 表示一個伺服器，這個類別是虛擬類別
    /// </summary>
    public abstract class Server
    {
        private string _name;

        public delegate void ServerInstallingEventHandler(object sender, InstallTask task);

        /// <summary>
        /// 當伺服器的名稱改變時觸發
        /// </summary>
        public event EventHandler? ServerNameChanged;

        /// <summary>
        /// 當伺服器的版本改變時觸發
        /// </summary>
        public event EventHandler? ServerVersionChanged;

        /// <summary>
        /// 當伺服器正在安裝軟體時觸發
        /// </summary>
        public event ServerInstallingEventHandler? ServerInstalling;

        // 內部空參數建構子 (防止有第三方伺服器軟體類別繼承自它)
        internal Server()
        {
            _name = string.Empty;
            ServerDirectory = string.Empty;
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
        public JsonPropertyFile? ServerInfoJson { get; private set; }

        /// <summary>
        /// 取得伺服器的執行環境資訊
        /// </summary>
        /// <returns>若無特殊的執行環境資訊，應回傳 <see langword="null"/> 來指示伺服器軟體執行者以預設環境執行</returns>
        public abstract RuntimeEnvironment? GetRuntimeEnvironment();

        /// <summary>
        /// 設定伺服器的執行環境資訊
        /// </summary>
        /// <returns></returns>
        public abstract void SetRuntimeEnvironment(RuntimeEnvironment? environment);

        /// <summary>        
        /// 載入位於指定路徑內的伺服器
        /// </summary>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server? LoadServer(string serverDirectory)
        {
            string path = Path.Combine(serverDirectory, "./server_info.json");
            if (!File.Exists(path))
                return null;
            JsonPropertyFile serverInformation = new JsonPropertyFile(path, useFileWatcher: false);
            string? softwareID = serverInformation["software"]?.GetValue<string>();
            if (softwareID is null)
                return null;
            return LoadServerCore(serverDirectory, serverInformation, softwareID);
        }

        /// <summary>
        /// 載入位於指定路徑內的伺服器，並將其指定為 <paramref name="softwareID"/> 所對應的伺服器軟體
        /// </summary>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <param name="software">伺服器軟體 ID</param>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server? LoadServer(string serverDirectory, string softwareID)
        {
            string path = Path.Combine(serverDirectory, "./server_info.json");
            if (!File.Exists(path))
                return null;
            return LoadServerCore(serverDirectory, new JsonPropertyFile(path, useFileWatcher: false), softwareID);
        }

        private static Server? LoadServerCore(string serverDirectory, JsonPropertyFile serverInfoJson, string softwareID)
        {
            Type? softwareType = SoftwareRegister.GetSoftwareTypeFromId(softwareID);
            if (softwareType is null)
                throw new ServerSoftwareIsNotRegisteredException(softwareID);
            object? newObj = Activator.CreateInstance(softwareType);
            if (newObj is not Server server)
            {
                (newObj as IDisposable)?.Dispose();
                return null;
            }
            serverDirectory = Path.GetFullPath(serverDirectory);
            server.ServerInfoJson = serverInfoJson;
            server.ServerDirectory = serverDirectory;
            server.ServerName = serverInfoJson["name"]?.GetValue<string>() ?? Path.GetDirectoryName(serverDirectory) ?? string.Empty;
            if (server.LoadServerCore(serverInfoJson))
                return server;
            (server as IDisposable)?.Dispose();
            return null;
        }

        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <typeparam name="T">軟體的型別</typeparam>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        public static T? CreateServer<T>(string serverDirectory) where T : Server
        {
            return CreateServerInternal(typeof(T), serverDirectory) as T;
        }

        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <param name="softwareType">軟體的型別</typeparam>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        public static Server? CreateServer(Type softwareType, string serverDirectory)
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
        public static Server? CreateServer(string softwareID, string serverDirectory)
        {
            Type? type = SoftwareRegister.GetSoftwareTypeFromId(softwareID);
            if (type is null)
                throw new ServerSoftwareIsNotRegisteredException(softwareID);
            return CreateServerInternal(type, serverDirectory);
        }

        private static Server? CreateServerInternal(Type softwareType, string serverDirectory)
        {
            object? newObj = Activator.CreateInstance(softwareType);
            if (newObj is not Server server)
            {
                (newObj as IDisposable)?.Dispose();
                return null;
            }
            server.ServerDirectory = serverDirectory;
            server.ServerName = GetDefaultServerName(server);
            if (server.CreateServer())
                return server;
            (server as IDisposable)?.Dispose();
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
        /// <param name="serverInfoJson">伺服器的資訊檔案</param>
        /// <returns>是否成功加載伺服器</returns>
        protected abstract bool LoadServerCore(JsonPropertyFile serverInfoJson);
        /// <summary>
        /// 子類別應覆寫此方法為建立伺服器的程式碼
        /// </summary>
        /// <returns>是否成功建立伺服器</returns>
        protected abstract bool CreateServer();
        /// <summary>
        /// 取得伺服器軟體ID
        /// </summary>
        public abstract string GetSoftwareId();
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
        /// 以 <see cref="GetRuntimeEnvironment"/> 內的執行環境來啟動伺服器
        /// </summary>
        /// <returns>伺服器是否已啟動</returns>
        public bool RunServer() => RunServer(GetRuntimeEnvironment());
        /// <summary>
        /// 以 <paramref name="environment"/> 所指定的執行環境來啟動伺服器
        /// </summary>
        /// <param name="environment">啟動伺服器時所要使用的執行環境，或是傳入 <see langword="null"/> 來指示其使用預設的執行環境</param>
        /// <returns>伺服器是否已啟動</returns>
        public abstract bool RunServer(RuntimeEnvironment? environment);
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
        /// <param name="serverInfoJson">伺服器的資訊檔案</param>
        /// <returns>是否成功儲存伺服器</returns>
        protected abstract bool SaveServerCore(JsonPropertyFile serverInfoJson);

        protected void OnServerInstalling(InstallTask task)
        {
            ServerInstalling?.Invoke(this, task);
        }

        protected virtual void OnServerVersionChanged()
        {
            ServerVersionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 儲存伺服器
        /// </summary>
        public void SaveServer()
        {
            JsonPropertyFile? serverInfoJson = ServerInfoJson;
            if (serverInfoJson is null)
            {
                serverInfoJson = new JsonPropertyFile(Path.Combine(ServerDirectory, "./server_info.json"), useFileWatcher: false);
                ServerInfoJson = serverInfoJson;
            }
            serverInfoJson["name"] = JsonValue.Create(ServerName);
            serverInfoJson["software"] = JsonValue.Create(GetSoftwareId());
            if (SaveServerCore(serverInfoJson))
                serverInfoJson.Save(false);
            IPropertyFile[] properties = GetServerPropertyFiles();
            if (properties is null)
                return;
            for (int i = 0, length = properties.Length; i < length; i++)
            {
                properties[i]?.Save(false);
            }
        }

        /// <summary>
        /// 伺服器物件的標籤，可供操作者儲存額外的伺服器資訊<br/>
        /// 伺服器軟體本身不應使用該屬性。
        /// </summary>
        public object? Tag { get; set; }
    }
}
