using System;
using System.Diagnostics;
using System.Text;
using DProcess = System.Diagnostics.Process;

namespace WitherTorch.Core
{
    public class SystemProcess : AbstractProcess, IDisposable
    {
        private bool disposedValue;

        public DProcess InnerProcess { get; protected set; }

        public override bool IsAlive()
        {
            InnerProcess?.Refresh();
            return InnerProcess != null && !InnerProcess.HasExited;
        }

        public virtual void StartProcess(ProcessStartInfo startInfo)
        {
            if (WTCore.RedirectSystemProcessStream)
            {
                startInfo.StandardOutputEncoding = Encoding.UTF8;
                startInfo.StandardErrorEncoding = Encoding.UTF8;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardInput = true;
            }
            DProcess process = DProcess.Start(startInfo);
            process.EnableRaisingEvents = true;
            if (startInfo.RedirectStandardInput)
            {
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e) { OnMessageRecived(process, new ProcessMessageEventArgs(true, e.Data == null ? "" : e.Data)); };
                process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) { OnMessageRecived(process, new ProcessMessageEventArgs(false, e.Data == null ? "" : e.Data)); };
                InputedCommand += delegate (object sender, InputedCommandEventArgs e) { if (!string.IsNullOrWhiteSpace(e.Message)) process.StandardInput.WriteLine(e.Message); };
            }
            process.Exited += delegate
            {
                InnerProcess?.Dispose();
                InnerProcess = null;
                OnProcessEnded(this);
            };
            InnerProcess = process;
            OnProcessStarted(this);
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
