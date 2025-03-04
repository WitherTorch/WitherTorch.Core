using System;
using System.IO;
using System.Runtime.CompilerServices;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Property
{
    /// <summary>
    /// <seealso cref="JavaPropertyFile"/>、<seealso cref="JsonPropertyFile"/> 和 <seealso cref="YamlPropertyFile"/> 的基底類別
    /// </summary>
    /// <typeparam name="TValue">設定檔案內所儲存的設定值類型</typeparam>
    public abstract class PropertyFileBase<TValue> : IPropertyFile
    {
        private readonly string _path;
        private readonly FileModifyWatcher? _watcher;

        private IPropertyFileDescriptor? _descriptor;

        private bool _loaded, _dirty, _disposed;

        /// <inheritdoc/>
        public string FilePath => _path;

        /// <summary>
        /// 取得該設定檔案所繫結的 <see cref="FileModifyWatcher"/> 物件
        /// </summary>
        public FileModifyWatcher? Watcher => _watcher;

        /// <inheritdoc/>
        public IPropertyFileDescriptor? Descriptor { get => _descriptor; set => _descriptor = value; }

        /// <summary>
        /// 以指定的設定檔路徑，建立新的 <see cref="PropertyFileBase{TValue}"/> 物件
        /// </summary>
        /// <param name="path">設定檔的路徑</param>
        public PropertyFileBase(string path) : this(path, WTCore.WatchPropertyFileModified) { }

        /// <summary>
        /// 以指定的設定檔路徑，建立新的 <see cref="PropertyFileBase{TValue}"/> 物件，並決定是否持續監測 <paramref name="path"/> 所對應的檔案狀態
        /// </summary>
        /// <param name="path">設定檔的路徑</param>
        /// <param name="useFileWatcher">是否持續監測 <paramref name="path"/> 所對應的檔案狀態</param>
        public PropertyFileBase(string path, bool useFileWatcher)
        {
            _path = path;
            FileModifyWatcher? watcher;
            if (useFileWatcher)
            {
                watcher = new FileModifyWatcher(path);
                watcher.Changed += FileWatcher_Changed;
                watcher.Active();
            }
            else
            {
                watcher = null;
            }
            _watcher = watcher;
        }

        /// <summary>
        /// 取得或修改設定檔案內的設定值
        /// </summary>
        /// <param name="key">該設定值所在的路徑</param>
        /// <returns><paramref name="key"/> 所對應的設定值，如果設定值不存在則為 <see langword="null"/></returns>
        /// <remarks>此屬性不會自動呼叫 <see cref="Save(bool)"/> 方法，如果修改了設定檔案的設定值，請手動呼叫一次上述方法來儲存設定檔案</remarks>
        public TValue? this[string? key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (key is null)
                    return default;
                Load(force: false);
                return GetValueCore(key);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (key is null)
                    return;
                Load(force: false);
                bool dirty;
                if (value is null)
                    dirty = RemoveValueCore(key);
                else
                    dirty = SetValueCore(key, value);
                if (dirty)
                    _dirty = true;
            }
        }

        /// <summary>
        /// 將設定檔案的內容載入至記憶體內
        /// </summary>
        /// <param name="force">是否在設定檔案已經處於載入狀態時強制重新載入</param>
        public void Load(bool force)
        {
            if (_loaded)
            {
                if (!force)
                    return;
                Unload();
            }
            string path = _path;
            Stream? stream = null;
            if (File.Exists(path))
            {
                try
                {
                    stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                catch (Exception)
                {
                }
            }
            if (stream is null)
            {
                LoadCore(null);
            }
            else
            {
                LoadCore(stream);
                stream.Dispose();
            }
            _loaded = true;
        }

        /// <summary>
        /// 將記憶體內的設定檔案內容清除
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unload()
        {
            _loaded = false;
            _dirty = false;
            UnloadCore();
        }

        /// <summary>
        /// 將該設定檔案標記為已修改
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>
        /// 將記憶體內的設定檔案內容儲存至原始檔案內
        /// </summary>
        /// <param name="force">是否在檔案未修改的情況下仍強制執行儲存操作</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Save(bool force)
        {
            if (!_loaded || (!force && !_dirty))
                return;
            FileModifyWatcher? watcher = _watcher;
            if (watcher is null)
            {
                Stream stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read);
                SaveCore(stream);
                stream.Dispose();
            }
            else
            {
                watcher.Changed -= FileWatcher_Changed;
                watcher.Deactive();
                Stream stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read);
                SaveCore(stream);
                stream.Dispose();
                watcher.Changed += FileWatcher_Changed;
                watcher.Active();
            }
            Unload();
        }

        /// <summary>
        /// 子類別應實作此方法為從檔案載入設定的程式碼
        /// </summary>
        /// <param name="stream">檔案的資料串流，如果檔案不存在則為 <see langword="null"/></param>
        protected abstract void LoadCore(Stream? stream);

        /// <summary>
        /// 子類別應實作此方法為從記憶體內清除暫存設定資料的程式碼
        /// </summary>
        protected abstract void UnloadCore();

        /// <summary>
        /// 子類別應實作此方法為將記憶體內設定資料存入檔案的程式碼
        /// </summary>
        /// <param name="stream">檔案的資料串流</param>
        protected abstract void SaveCore(Stream stream);

        /// <summary>
        /// 子類別應實作此方法為取得記憶體中特定設定值的程式碼
        /// </summary>
        /// <param name="key">要取得的設定路徑</param>
        /// <returns></returns>
        protected abstract TValue? GetValueCore(string key);

        /// <summary>
        /// 子類別應實作此方法為修改記憶體中特定設定值的程式碼
        /// </summary>
        /// <param name="key">要修改的設定路徑</param>
        /// <param name="value">要應用的設定值</param>
        /// <returns>是否成功修改設定</returns>
        protected abstract bool SetValueCore(string key, TValue value);

        /// <summary>
        /// 子類別應實作此方法為移除記憶體中特定設定值的程式碼
        /// </summary>
        /// <param name="key">要移除的設定路徑</param>
        /// <returns>是否成功移除設定</returns>
        protected abstract bool RemoveValueCore(string key);

        private void FileWatcher_Changed(object? sender, EventArgs e)
        {
            if (!_loaded || _dirty)
                return;
            Unload();
        }

        /// <inheritdoc/>
        public void Reload()
        {
            Load(force: true);
        }

        private void DisposeCore()
        {
            if (_disposed) 
                return;
            _disposed = true;
            _watcher?.Deactive();
        }

        /// <inheritdoc cref="object.Finalize()"/>
        ~PropertyFileBase()
        {
            DisposeCore();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }
    }
}
