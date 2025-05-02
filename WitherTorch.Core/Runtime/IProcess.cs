using System;

namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// <see cref="IProcess.MessageReceived"/> 事件專用的訊息委派
    /// </summary>
    /// <param name="sender">事件的傳送者 (可能為 <see langword="null"/>)</param>
    /// <param name="e">事件的額外資訊</param>
    public delegate void MessageReceivedEventHandler(object? sender, MessageReceivedEventArgs e);

    /// <summary>
    /// 伺服器處理序的基底介面
    /// </summary>
    public interface IProcess
    {
        /// <summary>
        /// 在處理序啟動時觸發的事件
        /// </summary>
        event EventHandler? ProcessStarted;

        /// <summary>
        /// 在處理序終止時觸發的事件
        /// </summary>
        event EventHandler? ProcessEnded;

        /// <summary>
        /// 在接收到處理序的執行訊息時觸發的事件
        /// </summary>                                                      
        event MessageReceivedEventHandler? MessageReceived;

        /// <summary>
        /// 取得當前處理序的 ID
        /// </summary>                                 
        int Id { get; }

        /// <summary>
        /// 如果當前處理序已經啟動，則為 <see langword="true"/>，否則為 <see langword="false"/>
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 取得當前處理序的開始時間
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// 對處理序輸入一行命令
        /// </summary>
        /// <param name="command">命令內容</param>
        void InputCommand(string command);

    }

    /// <summary>
    /// 提供 <see cref="IProcess.MessageReceived"/> 事件的額外資訊
    /// </summary>
    public sealed class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 指示此訊息是否是從錯誤訊息流傳出的
        /// </summary>
        public bool IsError { get; private set; }
        /// <summary>
        /// 訊息內容
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// <see cref="MessageReceivedEventArgs"/> 的建構子
        /// </summary>
        /// <param name="isError">標記此訊息是否為錯誤訊息</param>
        /// <param name="msg">此事件的訊息內容</param>
        public MessageReceivedEventArgs(bool isError, string msg)
        {
            IsError = isError;
            Message = msg;
        }
    }
}
