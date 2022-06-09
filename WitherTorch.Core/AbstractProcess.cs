using System;

namespace WitherTorch.Core
{
    public abstract class AbstractProcess
    {
        /// <summary>
        /// 取得當前處理序的 ID
        /// </summary>
        public int ID { get; set; }
        public delegate void ProcessMessageEventHandler(object sender, ProcessMessageEventArgs e);
        public event ProcessMessageEventHandler MessageRecived;
        public event EventHandler ProcessStarted;
        public event EventHandler ProcessEnded;

        public void OnMessageRecived(object sender, ProcessMessageEventArgs e = null)
        {
            MessageRecived?.Invoke(sender, e == null ? new ProcessMessageEventArgs() : e);
        }

        public void OnProcessStarted(object sender, EventArgs e = null)
        {
            ProcessStarted?.Invoke(sender, e ?? EventArgs.Empty);
        }

        public void OnProcessEnded(object sender, EventArgs e = null)
        {
            ProcessEnded?.Invoke(sender, e ?? EventArgs.Empty);
        }

        public virtual void InputCommand(string command = "")
        {
        }

        public abstract bool IsAlive();
    }
    public class ProcessMessageEventArgs : EventArgs
    {
        public bool IsError { get; private set; }
        public string Message { get; private set; }
        public ProcessMessageEventArgs(bool isError = false, string msg = "")
        {
            IsError = isError;
            Message = msg;
        }
    }

    public class Process : AbstractProcess
    {
        bool isAlive = false;
        public void SetAlive(bool alive)
        {
            isAlive = alive;
        }
        public override bool IsAlive()
        {
            return isAlive;
        }
    }
}
