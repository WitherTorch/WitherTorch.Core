using System;
using System.Diagnostics;
using System.Text;

using DProcess = System.Diagnostics.Process;

namespace WitherTorch.Core
{
    /// <summary>
    /// 可重覆使用的本機系統處理序類別
    /// </summary>
    public class SystemProcess : AbstractProcess, IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// 取得此處理序的系統處理序物件
        /// </summary>
        public DProcess? InnerProcess { get; protected set; }

        /// <inheritdoc/>
        public override int Id
        {
            get
            {
                DProcess? innerProcess = InnerProcess;
                if (innerProcess is null)
                    return default;
                try
                {
                    return innerProcess.Id;
                }
                catch (InvalidOperationException)
                {
                    return default;
                }
            }
        }

        /// <inheritdoc/>
        public override DateTime StartTime
        {
            get
            {
                DProcess? innerProcess = InnerProcess;
                if (innerProcess is null)
                    return default;
                try
                {
                    return innerProcess.StartTime;
                }
                catch (InvalidOperationException)
                {
                    return default;
                }
            }
        }

        /// <inheritdoc/>
        public override bool IsAlive
        {
            get
            {
                DProcess? innerProcess = InnerProcess;
                if (innerProcess is null)
                    return false;
                try
                {
                    return !innerProcess.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 強制停止已經啟動的本機系統處理序
        /// </summary>
        public void Kill()
        {
            DProcess? innerProcess = InnerProcess;
            if (innerProcess is null)
                return;
            InnerProcess = null;
            try
            {
                if (!innerProcess.HasExited)
                    innerProcess.Kill();
            }
            catch (InvalidOperationException)
            {
            }
            innerProcess.ErrorDataReceived -= Process_ErrorDataReceived;
            innerProcess.OutputDataReceived -= Process_OutputDataReceived;
            innerProcess.Exited -= Process_Exited;
            try
            {
                innerProcess.Dispose();
            }
            catch (InvalidOperationException)
            {
            }
            OnProcessEnded();
        }

        /// <inheritdoc/>
        public override void InputCommand(string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
                InnerProcess?.StandardInput.WriteLine(command);
        }

        /// <summary>
        /// 使用指定的 <see cref="ProcessStartInfo"/> 物件來啟動本機系統處理序
        /// </summary>
        /// <param name="startInfo">本機系統處理序的啟動資料</param>
        /// <returns>是否成功啟動本機系統處理序</returns>
        public virtual bool StartProcess(ProcessStartInfo startInfo)
        {
            if (WTCore.RedirectSystemProcessStream)
            {
                startInfo.StandardOutputEncoding = Encoding.UTF8;
                startInfo.StandardErrorEncoding = Encoding.UTF8;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardInput = true;
            }
            DProcess? process = DProcess.Start(startInfo);
            if (process is null || process.HasExited)
                return false;
            process.EnableRaisingEvents = true;
            if (startInfo.RedirectStandardInput)
            {
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.OutputDataReceived += Process_OutputDataReceived;
            }
            process.Exited += Process_Exited;
            InnerProcess = process;
            OnProcessStarted();
            return true;
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            DProcess? innerProcess = InnerProcess;
            if (!ReferenceEquals(innerProcess, sender) || innerProcess is null)
                return;
            InnerProcess = null;
            innerProcess.ErrorDataReceived -= Process_ErrorDataReceived;
            innerProcess.OutputDataReceived -= Process_OutputDataReceived;
            innerProcess.Exited -= Process_Exited;
            OnProcessEnded();
            innerProcess.Dispose();
        }

        private void Process_ErrorDataReceived(object? sender, DataReceivedEventArgs e)
        {
            OnMessageRecived(new MessageReceivedEventArgs(true, e.Data ?? string.Empty));
        }

        private void Process_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            OnMessageRecived(new MessageReceivedEventArgs(false, e.Data ?? string.Empty));
        }

        /// <inheritdoc cref="Dispose()"/>
        protected virtual void DisposeCore()
        {
            if (disposedValue)
                return;
            disposedValue = true;
            InnerProcess?.Dispose();
        }

        /// <summary>
        /// <see cref="SystemProcess"/> 的解構子
        /// </summary>
        ~SystemProcess()
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
