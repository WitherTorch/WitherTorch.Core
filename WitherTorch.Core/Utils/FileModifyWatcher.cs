using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Buffers;
using System.Diagnostics;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 提供持續監測檔案存在及修改狀態的服務
    /// </summary>
    public sealed class FileModifyWatcher
    {
        /// <summary>
        /// 當檔案有所變化時，觸發此事件
        /// </summary>
        public event EventHandler? Changed;

        private readonly string _path;

        private long _modifiedTime;

        /// <summary>
        /// <see cref="FileModifyWatcher"/> 的建構子
        /// </summary>
        /// <param name="path">要監測的檔案路徑</param>
        /// <exception cref="InvalidOperationException"></exception>
        public FileModifyWatcher(string path)
        {
            _path = Path.GetFullPath(path);
        }

        /// <summary>
        /// 更新 <see cref="FileModifyWatcher"/> 的狀態，並視需要觸發 <see cref="Changed"/> 事件
        /// </summary>
        public void UpdateState()
        {
            long newModifiedTime;
            string path = _path;
            if (File.Exists(path))
                newModifiedTime = File.GetLastWriteTimeUtc(path).Ticks;
            else
                newModifiedTime = 0L;
            if (_modifiedTime == newModifiedTime)
                return;
            _modifiedTime = newModifiedTime;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 啟動此 <see cref="FileModifyWatcher"/> 物件，並開始監聽對應的檔案
        /// </summary>
        public void Active() => WatchingThreadLoop.Instance.Add(this);

        /// <summary>
        /// 停止此 <see cref="FileModifyWatcher"/> 物件對特定檔案的監聽
        /// </summary>
        public void Deactive() => WatchingThreadLoop.Instance.Remove(this);

        private sealed class WatchingThreadLoop : CriticalFinalizerObject, IDisposable
        {
            private static readonly WatchingThreadLoop _instance = new WatchingThreadLoop();

            private readonly HashSet<FileModifyWatcher> _watchers;
            private readonly AutoResetEvent _trigger;

            private long _disposed;

            public static WatchingThreadLoop Instance => _instance;

            private WatchingThreadLoop()
            {
                _watchers = new HashSet<FileModifyWatcher>();
                _trigger = new AutoResetEvent(initialState: false);
                new Thread(DoLoop) { IsBackground = true, Name = nameof(FileModifyWatcher) + " ThreadLoop" }.Start();
            }

            public void Add(FileModifyWatcher watcher)
            {
                if (watcher is null)
                    return;
                HashSet<FileModifyWatcher> watchers = _watchers;
                Monitor.Enter(watchers);
                watchers.Add(watcher);
                Monitor.Exit(watchers);
                _trigger.Set();
            }

            public void Remove(FileModifyWatcher watcher)
            {
                if (watcher is null)
                    return;
                HashSet<FileModifyWatcher> watchers = _watchers;
                Monitor.Enter(watchers);
                watchers.Remove(watcher);
                Monitor.Exit(watchers);
            }

            private void DoLoop(object? obj)
            {
                const long ThreadLoopInterval = 500;

                ArrayPool<FileModifyWatcher> pool = ArrayPool<FileModifyWatcher>.Shared;
                Stopwatch stopwatch = new Stopwatch();

                HashSet<FileModifyWatcher> watchers = _watchers;
                AutoResetEvent trigger = _trigger;

                while (Interlocked.Read(ref _disposed) == 0L)
                {
                    stopwatch.Restart();
                    Monitor.Enter(watchers);
                    int count = watchers.Count;
                    if (count <= 0)
                    {
                        Monitor.Exit(watchers);
                        trigger.WaitOne();
                        continue;
                    }
                    FileModifyWatcher[] watcherArray = pool.Rent(count);
                    watchers.CopyTo(watcherArray, 0, count);
                    Monitor.Exit(watchers);
                    for (int i = 0; i < count; i++)
                        watcherArray[i].UpdateState();
                    pool.Return(watcherArray, clearArray: true);
                    stopwatch.Stop();
                    long elapsedTime = stopwatch.ElapsedMilliseconds;
                    if (elapsedTime >= ThreadLoopInterval)
                        continue;
                    if (elapsedTime <= 0)
                    {
                        Thread.Sleep(unchecked((int)ThreadLoopInterval));
                        continue;
                    }
                    Thread.Sleep(unchecked((int)(ThreadLoopInterval - elapsedTime)));
                }
                trigger.Dispose();
            }

            private void Dispose(bool disposing)
            {
                if (Interlocked.CompareExchange(ref _disposed, 1L, 0L) != 0L)
                    return;
                if (disposing)
                    _watchers.Clear();
                _trigger.Set();
            }

            ~WatchingThreadLoop()
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
}
