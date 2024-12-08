using System;
using System.IO;
using System.Runtime.CompilerServices;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Property
{
    public abstract class AbstractPropertyFile : IPropertyFile
    {
        private readonly string _path;

        private bool _isDirty;
        private bool _initialized;
        private bool _disposedValue;
        private IPropertyFileDescriptor? _descriptor;
        private FileWatcher? _watcher;

        protected AbstractPropertyFile(string path)
        {
            _path = path;
            _descriptor = null;
        }

        public string FilePath
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _path;
        }

        public IPropertyFileDescriptor? Descriptor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _descriptor;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _descriptor = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void MarkAsDirty() => _isDirty = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetFileWatching(bool value)
        {
            FileWatcher? watcher = _watcher;
            if (watcher is null)
                return;
            if (value)
                watcher.Changed += OnWatcherChanged;
            else
                watcher.Changed -= OnWatcherChanged;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;
            Initialize(_isDirty);
        }

        protected abstract void Initialize(bool isDirty);

        protected abstract void Reload(bool isDirty);

        protected abstract void Save(bool isDirty, bool force);

        public void Reload()
        {
            FileWatcher? watcher = _watcher;
            if (watcher is not null)
            {
                _watcher = null;
                watcher.Changed -= OnWatcherChanged;
            }
            if (WTCore.WatchPropertyFileModified)
            {
                watcher = new FileWatcher(_path);
                watcher.Changed += OnWatcherChanged;
                _watcher = watcher;
            }
            Reload(_isDirty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Save(bool force) => Save(_isDirty, force);

        protected virtual void ClearCache() { }

        protected virtual void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {
            if (_initialized && !_isDirty)
            {
                _initialized = false;
                ClearCache();
            }
        }

        protected virtual void DisposeManaged()
        {
            ClearCache();
        }

        protected virtual void DisposeUnmanaged() { }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    DisposeManaged();

                DisposeUnmanaged();
                _disposedValue = true;
            }
        }

        ~AbstractPropertyFile()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
