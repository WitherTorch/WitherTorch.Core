using System.Diagnostics;
using System.Text;
using DProcess = System.Diagnostics.Process;

namespace WitherTorch.Core
{
    public class SystemProcess : AbstractProcess
    {
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
                InnerProcess = null;
                OnProcessEnded(this);
            };
            InnerProcess = process;
            OnProcessStarted(this);
        }
    }
}
