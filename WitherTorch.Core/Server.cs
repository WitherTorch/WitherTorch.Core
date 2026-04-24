using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

using WitherTorch.Core.Property;
using WitherTorch.Core.Runtime;
using WitherTorch.Core.Tagging;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個伺服器，這個類別是抽象類別
    /// </summary>
    public abstract partial class Server
    {
        private readonly string _serverDirectory;
        private readonly List<IPersistentTag> _tagList = new();

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
        /// 在 <see cref="RunServer(IRuntimeEnvironment)"/> 被呼叫且準備啟動伺服器時觸發
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
        /// 以 <paramref name="environment"/> 所指定的執行環境來啟動伺服器
        /// </summary>
        /// <param name="environment">啟動伺服器時所要使用的執行環境</param>
        /// <returns>伺服器是否已啟動</returns>
        public abstract bool RunServer(IRuntimeEnvironment environment);

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
            serverInfoJson[ServerNameNode] = JsonValue.Create(ServerName);
            serverInfoJson[ServerSoftwareNode] = JsonValue.Create(GetSoftwareId());
            if (SaveServerCore(serverInfoJson))
            {
                SavePersistentTags(this, serverInfoJson);
                serverInfoJson.Save(false);
            }
            IPropertyFile[] properties = GetServerPropertyFiles();
            if (properties is null)
                return;
            for (int i = 0, length = properties.Length; i < length; i++)
            {
                properties[i]?.Save(false);
            }
        }

        /// <summary>
        /// 為伺服器物件附加 <paramref name="tag"/> 所指向的持久化標籤
        /// </summary>
        /// <param name="tag">要附加的持久化標籤 (如果傳入 <see langword="null"/> 則不會執行任何動作)</param>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void AddPersistentTag(IPersistentTag? tag)
        {
            if (tag is null)
                return;
            List<IPersistentTag> list = _tagList;
            lock (list)
                list.Add(tag);
        }

        /// <summary>
        /// 為伺服器物件附加 <paramref name="tags"/> 所指向的持久化標籤陣列
        /// </summary>
        /// <param name="tags">要附加的持久化標籤陣列 (如果傳入 <see langword="null"/> 則不會執行任何動作)</param>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void AddPersistentTags(IPersistentTag?[]? tags)
        {
            if (tags is null)
                return;
            int length = tags.Length;
            if (length <= 0)
                return;
            List<IPersistentTag> list = _tagList;
            lock (list)
            {
                foreach (IPersistentTag? tag in tags)
                {
                    if (tag is not null)
                        list.Add(tag);
                }
            }
        }

        /// <summary>
        /// 為伺服器物件附加 <paramref name="tags"/> 所指向的持久化標籤集合
        /// </summary>
        /// <param name="tags">要附加的持久化標籤集合 (如果傳入 <see langword="null"/> 則不會執行任何動作)</param>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void AddPersistentTags(IEnumerable<IPersistentTag?>? tags)
        {
            if (tags is null)
                return;
            using IEnumerator<IPersistentTag?> enumerator = tags.GetEnumerator();
            if (!enumerator.MoveNext())
                return;
            List<IPersistentTag> list = _tagList;
            lock (list)
            {
                do
                {
                    IPersistentTag? tag = enumerator.Current;
                    if (tag is not null)
                        list.Add(tag);
                } while (enumerator.MoveNext());
            }
        }

        /// <summary>
        /// 嘗試取得附加於伺服器物件且符合類型的持久化標籤
        /// </summary>
        /// <typeparam name="T">持久化標籤的類型</typeparam>
        /// <param name="result">成功取得的持久化標籤，或是 <see langword="null"/></param>
        /// <returns>是否成功取得指定的物件</returns>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public bool TryGetPersistentTag<T>([NotNullWhen(true)] out T? result) where T : IPersistentTag
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
            {
                foreach (IPersistentTag tag in list)
                {
                    if (tag is T castedTag)
                    {
                        result = castedTag;
                        return true;
                    }
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// 嘗試取得附加於伺服器物件且符合條件的持久化標籤
        /// </summary>
        /// <param name="predicate">要篩選的條件</param>
        /// <param name="result">成功取得的持久化標籤，或是 <see langword="null"/></param>
        /// <returns>是否成功取得指定的物件</returns>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public bool TryGetPersistentTag(Predicate<IPersistentTag> predicate, [NotNullWhen(true)] out IPersistentTag? result)
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
            {
                foreach (IPersistentTag tag in list)
                {
                    if (predicate.Invoke(tag))
                    {
                        result = tag;
                        return true;
                    }
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// 嘗試取得附加於伺服器物件且符合條件的持久化標籤
        /// </summary>
        /// <param name="predicate">要篩選的條件</param>
        /// <param name="state">傳入 <paramref name="predicate"/> 的狀態物件</param>
        /// <param name="result">成功取得的持久化標籤，或是 <see langword="null"/></param>
        /// <returns>是否成功取得指定的物件</returns>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public bool TryGetPersistentTag<TState>(Func<IPersistentTag, TState, bool> predicate, TState state, [NotNullWhen(true)] out IPersistentTag? result)
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
            {
                foreach (IPersistentTag tag in list)
                {
                    if (predicate.Invoke(tag, state))
                    {
                        result = tag;
                        return true;
                    }
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// 取得所有附加於伺服器物件的持久化標籤
        /// </summary>
        /// <returns>成功取得的持久化標籤陣列</returns>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public IPersistentTag[] GetPersistentTags()
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
            {
                int count = 0;
                if (count <= 0)
                    return Array.Empty<IPersistentTag>();
                return list.ToArray();
            }
        }

        /// <summary>
        /// 取得所有附加於伺服器物件且符合條件的持久化標籤
        /// </summary>
        /// <param name="predicate">要篩選的條件</param>
        /// <returns>成功取得的持久化標籤陣列</returns>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public IPersistentTag[] GetPersistentTags(Func<IPersistentTag, bool> predicate)
        {
            ArrayPool<IPersistentTag> pool = ArrayPool<IPersistentTag>.Shared;
            List<IPersistentTag> list = _tagList;
            IPersistentTag[] buffer;
            int resultCount = 0;
            lock (list)
            {
                int count = 0;
                if (count <= 0)
                    return Array.Empty<IPersistentTag>();
                buffer = pool.Rent(count);
                try
                {
                    foreach (IPersistentTag tag in list)
                    {
                        if (!predicate.Invoke(tag))
                            continue;
                        buffer[resultCount++] = tag;
                    }
                }
                catch (Exception)
                {
                    pool.Return(buffer, clearArray: true);
                    throw;
                }
            }
            try
            {
                if (resultCount <= 0)
                    return Array.Empty<IPersistentTag>();
                IPersistentTag[] result = new IPersistentTag[resultCount];
                Array.Copy(buffer, result, resultCount);
                return result;
            }
            finally
            {
                pool.Return(buffer, clearArray: true);
            }
        }

        /// <summary>
        /// 取得所有附加於伺服器物件且符合條件的持久化標籤
        /// </summary>
        /// <param name="predicate">要篩選的條件</param>
        /// <param name="state">傳入 <paramref name="predicate"/> 的狀態物件</param>
        /// <returns>成功取得的持久化標籤陣列</returns>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public IPersistentTag[] GetPersistentTags<TState>(Func<IPersistentTag, TState, bool> predicate, TState state)
        {
            ArrayPool<IPersistentTag> pool = ArrayPool<IPersistentTag>.Shared;
            List<IPersistentTag> list = _tagList;
            IPersistentTag[] buffer;
            int resultCount = 0;
            lock (list)
            {
                int count = 0;
                if (count <= 0)
                    return Array.Empty<IPersistentTag>();
                buffer = pool.Rent(count);
                try
                {
                    foreach (IPersistentTag tag in list)
                    {
                        if (!predicate.Invoke(tag, state))
                            continue;
                        buffer[resultCount++] = tag;
                    }
                }
                catch (Exception)
                {
                    pool.Return(buffer, clearArray: true);
                    throw;
                }
            }
            try
            {
                if (resultCount <= 0)
                    return Array.Empty<IPersistentTag>();
                IPersistentTag[] result = new IPersistentTag[resultCount];
                Array.Copy(buffer, result, resultCount);
                return result;
            }
            finally
            {
                pool.Return(buffer, clearArray: true);
            }
        }

        /// <summary>
        /// 清除所有附加在伺服器物件的持久化標籤
        /// </summary>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void ClearPersistentTags()
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
                list.Clear();
        }

        /// <summary>
        /// 為伺服器物件移除 <paramref name="tag"/> 所指向的持久化標籤
        /// </summary>
        /// <param name="tag">要移除的持久化標籤 (如果傳入 <see langword="null"/> 則不會執行任何動作)</param>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void RemovePersistentTag(IPersistentTag? tag)
        {
            if (tag is null)
                return;
            List<IPersistentTag> list = _tagList;
            lock (list)
                list.Remove(tag);
        }

        /// <summary>
        /// 為伺服器物件移除所有指定類型的持久化標籤
        /// </summary>
        /// <typeparam name="T">持久化標籤的類型</typeparam>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void RemovePersistentTags<T>() where T : IPersistentTag
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
                RemovePersistentTags(static val => val is T);
        }

        /// <summary>
        /// 為伺服器物件移除所有符合條件的持久化標籤
        /// </summary>
        /// <param name="predicate">要篩選的條件</param>
        /// <remarks>
        /// 備註: 由於持久化標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void RemovePersistentTags(Predicate<IPersistentTag> predicate)
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
                list.RemoveAll(predicate);
        }

        /// <summary>
        /// 為伺服器物件移除所有符合條件的持久化標籤
        /// </summary>
        /// <param name="predicate">要篩選的條件</param>
        /// <param name="state">傳入 <paramref name="predicate"/> 的狀態物件</param>
        /// <remarks>
        /// 備註: 由於標籤物件列表可被使用者更動，故伺服器軟體本身若使用此處進行持久狀態存儲時需考慮被移除之可能性。
        /// </remarks>
        public void RemoveTags<TState>(Func<IPersistentTag, TState, bool> predicate, TState state)
        {
            List<IPersistentTag> list = _tagList;
            lock (list)
            {
                int count = list.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    IPersistentTag tag = list[i];
                    if (predicate.Invoke(tag, state))
                        list.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 伺服器物件的非持久化標籤，可供使用者儲存額外的伺服器資訊
        /// </summary>
        /// <remarks>
        /// 伺服器軟體本身不應使用該屬性。
        /// </remarks>
        public object? Tag { get; set; }
    }
}
