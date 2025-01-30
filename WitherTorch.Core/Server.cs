using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

using WitherTorch.Core.Property;
using WitherTorch.Core.Software;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個伺服器，這個類別是虛擬類別
    /// </summary>
    public abstract class Server
    {
        private readonly string _serverDirectory;
        private string _name = string.Empty;

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
        /// 在 <see cref="RunServer"/> 或 <see cref="RunServer(RuntimeEnvironment?)"/> 被呼叫且準備啟動伺服器時觸發
        /// </summary>
        public event EventHandler? BeforeRunServer;

        /// <summary>
        /// 伺服器名稱
        /// </summary>
        public string ServerName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _name;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _name = value;
                OnServerNameChanged();
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
        public string ServerDirectory => _serverDirectory;

        /// <summary>
        /// 取得人類可讀(human-readable)的軟體版本
        /// </summary>
        public abstract string GetReadableVersion();

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

        protected Server(string serverDirectory)
        {
            _serverDirectory = serverDirectory;
        }

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
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(serverInformation["software"]?.GetValue<string>(), throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            return LoadServerCore(software, serverDirectory, serverInformation);
        }

        /// <summary>
        /// 載入位於指定路徑內的伺服器，並將其指定為 <paramref name="softwareId"/> 所對應的伺服器軟體
        /// </summary>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <param name="software">伺服器軟體 ID</param>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server? LoadServer(string serverDirectory, string softwareId)
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(softwareId, throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            string path = Path.Combine(serverDirectory, "./server_info.json");
            if (!File.Exists(path))
                return null;
            return LoadServerCore(software, serverDirectory, new JsonPropertyFile(path, useFileWatcher: false));
        }

        private static Server? LoadServerCore(ISoftwareContext factory, string serverDirectory, JsonPropertyFile serverInfoJson)
        {
            Server? server = factory.CreateServerInstance(serverDirectory);
            if (server is null)
                return null;
            if (!factory.GetServerType().IsAssignableFrom(server.GetType()))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            server.ServerInfoJson = serverInfoJson;
            server.ServerName = serverInfoJson["name"]?.GetValue<string>() ?? GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.LoadServerCore(serverInfoJson))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        /// <summary>
        /// 以指定的伺服器物件類型與伺服器資料夾路徑來建立新的伺服器
        /// </summary>
        /// <typeparam name="T">伺服器類型，需使用 <see cref="SoftwareRegister.TryRegisterServerSoftware(ISoftwareContext)"/> 註冊</typeparam>
        /// <param name="serverDirectory">伺服器資料夾的路徑</param>
        /// <returns>建立好的伺服器，或是 <see langword="null"/> (如果建立伺服器時發生問題的話)</returns>
        public static T? CreateServer<T>(string serverDirectory) where T : Server
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(typeof(T), throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            Server? server = CreateServerCore(software, serverDirectory);
            if (server is not T result)
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return result;
        }

        /// <summary>
        /// 以指定的伺服器物件類型與伺服器資料夾路徑來建立新的伺服器
        /// </summary>
        /// <param name="serverType">伺服器類型，需繼承 <see cref="Server"/> 並使用 <see cref="SoftwareRegister.TryRegisterServerSoftware(ISoftwareContext)"/> 註冊</param>
        /// <param name="serverDirectory">伺服器資料夾的路徑</param>
        /// <returns>建立好的伺服器，或是 <see langword="null"/> (如果建立伺服器時發生問題的話)</returns>
        public static Server? CreateServer(Type serverType, string serverDirectory)
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(serverType, throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            return CreateServerCore(software, serverDirectory);
        }

        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <param name="softwareId">軟體的ID</param>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        public static Server? CreateServer(string softwareId, string serverDirectory)
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(softwareId, throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            return CreateServerCore(software, serverDirectory);
        }

        private static Server? CreateServerCore(ISoftwareContext factory, string serverDirectory)
        {
            Server? server = factory.CreateServerInstance(serverDirectory);
            if (server is null)
                return null;
            if (!factory.GetServerType().IsAssignableFrom(server.GetType()))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            server.ServerName = GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.CreateServerCore())
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        /// <summary>
        /// 取得預設的伺服器名稱 (通常是伺服器資料夾的名字)
        /// </summary>
        /// <returns>預設的伺服器名稱</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDefaultServerName(Server server)
            => GetDefaultServerNameCore(server.ServerDirectory);

        private static string GetDefaultServerNameCore(string serverDirectory)
        {
#if NET5_0_OR_GREATER
            ReadOnlySpan<char> span = serverDirectory.AsSpan().TrimEnd(Path.DirectorySeparatorChar);
            if (span.Length > 3)
                return Path.GetFileName(span).ToString();
#else
            serverDirectory = serverDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if (serverDirectory.Length > 3)
                return Path.GetFileName(serverDirectory);
#endif
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
        protected abstract bool CreateServerCore();

        /// <summary>
        /// 取得伺服器軟體ID
        /// </summary>
        public abstract string GetSoftwareId();

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
        /// 生成一個裝載伺服器安裝流程的 <see cref="InstallTask"/> 物件
        /// </summary>
        /// <param name="version">要安裝的軟體版本</param>
        /// <returns>如果成功裝載安裝流程，則為一個有效的 <see cref="InstallTask"/> 物件，否則會回傳 <see langword="null"/></returns>
        public abstract InstallTask? GenerateInstallServerTask(string version);

        /// <summary>
        /// 生成一個裝載伺服器更新流程的 <see cref="InstallTask"/> 物件
        /// </summary>
        /// <param name="version">要安裝的軟體版本</param>
        /// <returns>如果成功裝載更新流程，則為一個有效的 <see cref="InstallTask"/> 物件，否則會回傳 <see langword="null"/></returns>
        public virtual InstallTask? GenerateUpdateServerTask() => GenerateInstallServerTask(ServerVersion);

        /// <summary>
        /// 子類別應覆寫此方法為儲存伺服器的程式碼
        /// </summary>
        /// <param name="serverInfoJson">伺服器的資訊檔案</param>
        /// <returns>是否成功儲存伺服器</returns>
        protected abstract bool SaveServerCore(JsonPropertyFile serverInfoJson);

        protected virtual void OnServerNameChanged()
        {
            ServerNameChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnServerVersionChanged()
        {
            ServerVersionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnBeforeRunServer()
        {
            BeforeRunServer?.Invoke(this, EventArgs.Empty);
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
