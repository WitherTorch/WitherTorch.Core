﻿using System;
using System.Diagnostics;
using System.Text;

using DProcess = System.Diagnostics.Process;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個本機系統處理序
    /// </summary>
    public class SystemProcess : AbstractProcess, IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// 取得此處理序的系統處理序物件
        /// </summary>
        public DProcess? InnerProcess { get; protected set; }

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                }
                InnerProcess?.Dispose();
                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~SystemProcess()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
