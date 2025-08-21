using System;
using System.Text;
using System.Threading;

namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// 可重覆使用的本機系統處理序類別
    /// </summary>
    public class LocalProcess : ILocalProcess
    {
        private System.Diagnostics.Process? _process;
        private bool _disposed;

        /// <inheritdoc/>
        public event EventHandler? ProcessStarted;
        /// <inheritdoc/>
        public event EventHandler? ProcessEnded;
        /// <inheritdoc/>
        public event MessageReceivedEventHandler? MessageReceived;

        /// <inheritdoc/>
        public int Id
        {
            get
            {
                System.Diagnostics.Process? process = _process;
                if (process is null)
                    return default;
                try
                {
                    return process.Id;
                }
                catch (InvalidOperationException)
                {
                    return default;
                }
            }
        }

        /// <inheritdoc/>
        public DateTime StartTime
        {
            get
            {
                System.Diagnostics.Process? process = _process;
                if (process is null)
                    return default;
                try
                {
                    return process.StartTime;
                }
                catch (InvalidOperationException)
                {
                    return default;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsAlive
        {
            get
            {
                System.Diagnostics.Process? process = _process;
                if (process is null)
                    return false;
                try
                {
                    return !process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        /// <inheritdoc />
        public string? WorkingDirectory
        {
            get
            {
                System.Diagnostics.Process? process = _process;
                if (process is null)
                    return null;
                try
                {
                    return process.StartInfo.WorkingDirectory;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            System.Diagnostics.Process? process;
            if ((process = Interlocked.Exchange(ref _process, null)) is null)
                return;
            StopCore(process);
        }

        /// <summary>
        /// 終止指定的本機系統處理序
        /// </summary>
        /// <param name="process">要終止的本機系統處理序</param>
        protected virtual void StopCore(System.Diagnostics.Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch (InvalidOperationException)
            {
            }
            process.ErrorDataReceived -= Process_ErrorDataReceived;
            process.OutputDataReceived -= Process_OutputDataReceived;
            process.Exited -= Process_Exited;
            ProcessEnded?.Invoke(this, EventArgs.Empty);
            try
            {
                process.Dispose();
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <inheritdoc/>
        public void InputCommand(string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
                _process?.StandardInput.WriteLine(command);
        }

        /// <inheritdoc />
        public System.Diagnostics.Process? AsCLRProcess() => _process;

        /// <inheritdoc />
        public bool Start(in LocalProcessStartInfo startInfo)
        {
            System.Diagnostics.ProcessStartInfo processStartInfo = startInfo.ToProcessStartInfo();
            if (WTCore.RedirectSystemProcessStream)
            {
                processStartInfo.StandardOutputEncoding = Encoding.UTF8;
                processStartInfo.StandardErrorEncoding = Encoding.UTF8;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardInput = true;
            }
            System.Diagnostics.Process? process = System.Diagnostics.Process.Start(processStartInfo);
            if (process is null || process.HasExited)
                return false;

            System.Diagnostics.Process? oldProcess;
            if ((oldProcess = Interlocked.CompareExchange(ref _process, process, null)) is not null)
            {
                try
                {
                    if (!oldProcess.HasExited)
                    {
                        process.Kill();
                        process.Dispose();
                        return false;
                    }
                }
                catch (Exception)
                {
                }
                System.Diagnostics.Process? secondCheckOldProcess;
                while (!ReferenceEquals(secondCheckOldProcess = Interlocked.CompareExchange(ref _process, process, oldProcess), oldProcess))
                {
                    oldProcess.Dispose();
                    try
                    {
                        if (!secondCheckOldProcess.HasExited)
                        {
                            process.Kill();
                            process.Dispose();
                            return false;
                        }
                    }
                    catch (Exception)
                    {
                    }
                    oldProcess = secondCheckOldProcess;
                    break;
                }
            }

            StartCore(process);
            ProcessStarted?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// 在指定的本機系統處理序啟動之後要執行的程式碼
        /// </summary>
        /// <param name="process">已啟動的本機系統處理序</param>
        protected virtual void StartCore(System.Diagnostics.Process process)
        {
            process.EnableRaisingEvents = true;
            if (process.StartInfo.RedirectStandardInput)
            {
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.OutputDataReceived += Process_OutputDataReceived;
            }
            process.Exited += Process_Exited;
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            if (sender is not System.Diagnostics.Process process || 
                !ReferenceEquals(Interlocked.CompareExchange(ref _process, null, process), process))
                return;
            StopCore(process);
        }

        private void Process_ErrorDataReceived(object? sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(true, e.Data ?? string.Empty));
        }

        private void Process_OutputDataReceived(object? sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(false, e.Data ?? string.Empty));
        }

        /// <inheritdoc cref="Dispose()"/>
        protected virtual void DisposeCore()
        {
            if (_disposed)
                return;
            _disposed = true;
            Stop();
        }

        /// <summary>
        /// <see cref="LocalProcess"/> 的解構子
        /// </summary>
        ~LocalProcess()
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
