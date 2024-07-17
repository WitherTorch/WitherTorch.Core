using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
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

        public event FileSystemEventHandler Changed;

        private readonly string _path;
        private readonly FileSystemWatcherData data;
        private bool disposedValue;

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
            string dirPath = Path.GetDirectoryName(path);
            data = watcherDict.AddOrUpdate(dirPath, _path => new FileSystemWatcherData(_path), (_, _data) =>
            {
                _data.Ref();
                return _data;
            });
            data.watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileSystemEventHandler handlers = Changed;
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

                data.Unref();
                disposedValue = true;
            }
        }

        ~FileWatcher()
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
