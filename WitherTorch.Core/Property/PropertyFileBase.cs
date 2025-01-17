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
        private readonly FileWatcher? _watcher;

        private IPropertyFileDescriptor? _descriptor;

        private bool _loaded, _dirty;

        public string FilePath => _path;

        public FileWatcher? Watcher => _watcher;

        public IPropertyFileDescriptor? Descriptor { get => _descriptor; set => _descriptor = value; }

        public PropertyFileBase(string path) : this(path, WTCore.WatchPropertyFileModified) { }

        public PropertyFileBase(string path, bool useFileWatcher)
        {
            _path = path;
            FileWatcher? watcher;
            if (useFileWatcher)
            {
                watcher = new FileWatcher(path);
                watcher.Changed += FileWatcher_Changed;
            }
            else
            {
                watcher = null;
            }
            _watcher = watcher;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unload()
        {
            _loaded = false;
            _dirty = false;
            UnloadCore();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkDirty()
        {
            _dirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Save(bool force)
        {
            if (!_loaded || (!force && !_dirty))
                return;
            FileWatcher? watcher = _watcher;
            if (watcher is null)
            {
                Stream stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read);
                SaveCore(stream);
                stream.Dispose();
            }
            else
            {
                watcher.Changed -= FileWatcher_Changed;
                Stream stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read);
                SaveCore(stream);
                stream.Dispose();
                watcher.Changed += FileWatcher_Changed;
            }
            Unload();
        }

        protected abstract void LoadCore(Stream? stream);

        protected abstract void UnloadCore();

        protected abstract void SaveCore(Stream stream);

        protected abstract TValue? GetValueCore(string key);

        protected abstract bool SetValueCore(string key, TValue value);

        protected abstract bool RemoveValueCore(string key);

        protected void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!_loaded || _dirty)
                return;
            Unload();
        }

        public void Reload()
        {
            Load(force: true);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
