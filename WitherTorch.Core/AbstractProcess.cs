using System;
using System.Runtime.CompilerServices;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個處理序，這是一個抽象類別
    /// </summary>
    public abstract class AbstractProcess
    {
        /// <summary>
        /// 取得當前處理序的 ID
        /// </summary>                                 
        public abstract int Id { get; }

        /// <summary>
        /// 如果當前處理序已經啟動，則為 <see langword="true"/>，否則為 <see langword="false"/>
        /// </summary>
        public abstract bool IsAlive { get; }

        /// <summary>
        /// 取得當前處理序的開始時間
        /// </summary>
        public abstract DateTime StartTime { get; }

        public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs e);

        /// <summary>
        /// 在接收到處理序的執行訊息時觸發
        /// </summary>                                                      
        public event MessageReceivedEventHandler MessageReceived;

        /// <summary>
        /// 在處理序啟動時觸發
        /// </summary>
        public event EventHandler ProcessStarted;

        /// <summary>
        /// 在處理序終止時觸發
        /// </summary>
        public event EventHandler ProcessEnded;


        /// <summary>
        /// 觸發 <see cref="MessageReceived"/> 事件
        /// </summary>
        /// <param name="e">事件內容</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnMessageRecived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        /// <summary>
        /// 觸發 <see cref="ProcessStarted"/> 事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnProcessStarted()
        {
            ProcessStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 觸發 <see cref="ProcessEnded"/> 事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnProcessEnded()
        {
            ProcessEnded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 對處理序輸入一行命令
        /// </summary>
        /// <param name="command">命令內容</param>
        public abstract void InputCommand(string command);
    }
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 指示此訊息是否是從錯誤訊息流傳出的
        /// </summary>
        public bool IsError { get; private set; }
        /// <summary>
        /// 訊息內容
        /// </summary>
        public string Message { get; private set; }

        public MessageReceivedEventArgs(bool isError, string msg)
        {
            IsError = isError;
            Message = msg;
        }
    }

    /// <summary>
    /// <see cref="AbstractProcess"/> 類別的簡易實作
    /// </summary>
    public class SimpleProcess : AbstractProcess
    {
        bool isAlive = false;
        DateTime start;

        public override int Id { get; }

        /// <summary>
        /// 設定此處理序是否正在運行
        /// </summary>
        /// <param name="alive"></param>
        public void SetAlive(bool alive)
        {
            isAlive = alive;
            if (alive) start = DateTime.Now;
        }

        /// <inheritdoc/>
        public override bool IsAlive
        {
            get
            {
                return isAlive;
            }
        }

        public override DateTime StartTime => start;

        /// <inheritdoc/>
        public override void InputCommand(string command)
        {
            Console.WriteLine(command);
        }
    }
}
