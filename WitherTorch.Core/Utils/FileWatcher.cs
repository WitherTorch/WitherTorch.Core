using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 提供持續監測檔案狀態的服務
    /// </summary>
    public sealed class FileWatcher : IDisposable
    {
        private sealed class FileSystemWatcherData : IDisposable
        {
            public readonly string path;
            public readonly FileSystemWatcher watcher;
            private int refCount;

            public FileSystemWatcherData(string path)
            {
                this.path = path;
                watcher = new FileSystemWatcher(path, "*.*")
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };
                refCount = 1;
            }

            public void Dispose()
            {
                watcherDict.TryRemove(path, out _);
                watcher.Dispose();
            }

            public void Unref()
            {
                int newRefCount = Interlocked.Decrement(ref refCount);
                if (newRefCount == 0)
                {
                    Dispose();
                }
            }

            public void Ref()
            {
                Interlocked.Increment(ref refCount);
            }
        }

        private static readonly ConcurrentDictionary<string, FileSystemWatcherData> watcherDict = new ConcurrentDictionary<string, FileSystemWatcherData>();

        /// <summary>
        /// 當檔案有所變化時，觸發此事件
        /// </summary>
        public event FileSystemEventHandler? Changed;

        private readonly string _path;
        private readonly FileSystemWatcherData data;
        private bool disposedValue;

        /// <summary>
        /// <see cref="FileWatcher"/> 的建構子
        /// </summary>
        /// <param name="path">要監測的檔案路徑</param>
        /// <exception cref="InvalidOperationException"></exception>
        public FileWatcher(string path)
        {
            path = Path.GetFullPath(path);
#if NET5_0_OR_GREATER
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                path = path.ToLower();
#else
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                path = path.ToLower();
#endif
            _path = path;
            string? dirPath = Path.GetDirectoryName(path);
            if (dirPath is null)
                throw new InvalidOperationException();
            data = watcherDict.AddOrUpdate(dirPath, _path => new FileSystemWatcherData(_path), (_, _data) =>
            {
                _data.Ref();
                return _data;
            });
            data.watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileSystemEventHandler? handlers = Changed;
            if (handlers is null)
                return;
            if (string.Equals(Path.GetFullPath(e.FullPath), _path, StringComparison.OrdinalIgnoreCase))
            {
                handlers.Invoke(this, e);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    data.watcher.Changed -= Watcher_Changed;
                }

                data?.Unref();
                disposedValue = true;
            }
        }

        /// <summary>
        /// <see cref="FileWatcher"/> 的解構子
        /// </summary>
        ~FileWatcher()
        {
            Dispose(disposing: false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
