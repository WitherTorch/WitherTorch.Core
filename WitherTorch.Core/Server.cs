using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

using WitherTorch.Core.Property;
using WitherTorch.Core.Runtime;
using WitherTorch.Core.Software;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個伺服器，這個類別是抽象類別
    /// </summary>
    public abstract partial class Server
    {
        private readonly string _serverDirectory;
        private string _name = string.Empty;

        /// <summary>
        /// 當伺服器的名稱改變時觸發
        /// </summary>
        public event EventHandler? ServerNameChanged;

        /// <summary>
        /// 當伺服器的版本改變時觸發
        /// </summary>
        public event EventHandler? ServerVersionChanged;

        /// <summary>
        /// 在 <see cref="RunServer()"/> 或 <see cref="RunServer(RuntimeEnvironment?)"/> 被呼叫且準備啟動伺服器時觸發
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

        /// <summary>
        /// <see cref="Server"/> 的建構子
        /// </summary>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        protected Server(string serverDirectory)
        {
            _serverDirectory = serverDirectory;
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
        public abstract IProcess GetProcess();

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
        /// <returns>如果成功裝載更新流程，則為一個有效的 <see cref="InstallTask"/> 物件，否則會回傳 <see langword="null"/></returns>
        public virtual InstallTask? GenerateUpdateServerTask() => GenerateInstallServerTask(ServerVersion);

        /// <summary>
        /// 子類別應覆寫此方法為儲存伺服器的程式碼
        /// </summary>
        /// <param name="serverInfoJson">伺服器的資訊檔案</param>
        /// <returns>是否成功儲存伺服器</returns>
        protected abstract bool SaveServerCore(JsonPropertyFile serverInfoJson);

        /// <summary>
        /// 觸發 <see cref="ServerNameChanged"/> 事件
        /// </summary>
        protected virtual void OnServerNameChanged()
        {
            ServerNameChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 觸發 <see cref="ServerVersionChanged"/> 事件
        /// </summary>
        protected virtual void OnServerVersionChanged()
        {
            ServerVersionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 觸發 <see cref="BeforeRunServer"/> 事件
        /// </summary>
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
