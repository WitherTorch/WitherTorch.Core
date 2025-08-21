using System;

using WitherTorch.Core.Runtime;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一種特定的安裝狀態，此類別為抽象類別
    /// </summary>
    public abstract class AbstractInstallStatus
    {
        /// <summary>
        /// 當狀態物件的內容改變時觸發
        /// </summary>
        public event EventHandler? Updated;

        /// <summary>
        /// 觸發 <see cref="Updated"/> 事件
        /// </summary>
        protected void OnUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 通過其他子處理序進行安裝的狀態類別
    /// </summary>
    public class ProcessStatus : AbstractInstallStatus
    {
        /// <summary>
        /// 當接收到子處理序傳回的輸出訊息時觸發
        /// </summary>
        public event MessageReceivedEventHandler? ProcessMessageReceived;

        private double _percentage;

        /// <summary>
        /// 取得或設定目前 <see cref="ProcessStatus"/> 的進度
        /// </summary>
        public double Percentage
        {
            get
            {
                return _percentage;
            }
            set
            {
                _percentage = value;
                OnUpdated();
            }
        }

        /// <summary>
        /// <see cref="ProcessStatus"/> 的建構子
        /// </summary>
        /// <param name="percentage">此狀態的初始進度</param>
        public ProcessStatus(double percentage)
        {
            _percentage = percentage;
        }

        /// <summary>
        /// 觸發 <see cref="ProcessMessageReceived"/> 事件
        /// </summary>
        /// <param name="sender">事件的傳送者 (可能為 <see langword="null"/>)</param>
        /// <param name="e">事件的額外資訊</param>
        public virtual void OnProcessMessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            ProcessMessageReceived?.Invoke(sender, e);
        }
    }

    /// <summary>
    /// 下載檔案的狀態類別
    /// </summary>
    public class DownloadStatus : AbstractInstallStatus
    {
        private double _percentage;

        /// <summary>
        /// 檔案的下載路徑 (一般為網址)
        /// </summary>
        public string DownloadFrom { get; }

        /// <summary>
        /// 取得或設定檔案的下載進度
        /// </summary>
        public double Percentage
        {
            get
            {
                return _percentage;
            }
            set
            {
                _percentage = value;
                OnUpdated();
            }
        }

        /// <summary>
        /// <see cref="DownloadStatus"/> 的建構子
        /// </summary>
        /// <param name="from">檔案的下載路徑 (一般為網址)</param>
        /// <param name="percentage">檔案的初始下載進度</param>
        public DownloadStatus(string from, double percentage = 0.0)
        {
            DownloadFrom = from;
            Percentage = percentage;
        }
    }

    /// <summary>
    /// 解壓縮檔案的狀態類別
    /// </summary>
    public class DecompessionStatus : AbstractInstallStatus
    {
        private double _percentage;

        /// <summary>
        /// 取得或設定檔案的解壓縮進度
        /// </summary>
        public double Percentage
        {
            get
            {
                return _percentage;
            }
            set
            {
                _percentage = value;
                OnUpdated();
            }
        }

        /// <summary>
        /// <see cref="DecompessionStatus"/> 的建構子
        /// </summary>
        /// <param name="percentage">檔案的初始解壓縮進度</param>
        public DecompessionStatus(double percentage = 0.0)
        {
            Percentage = percentage;
        }
    }

    /// <summary>
    /// 準備安裝時的狀態類別，此類別無法建立實例
    /// </summary>
    public sealed class PreparingInstallStatus : AbstractInstallStatus
    {
        /// <summary>
        /// <see cref="PreparingInstallStatus"/> 的唯一實例，用於指示"準備安裝"的狀態
        /// </summary>
        public static readonly PreparingInstallStatus Instance = new PreparingInstallStatus();

        private PreparingInstallStatus() { }
    }

    /// <summary>
    /// 表示正在驗證檔案的狀態
    /// </summary>
    public sealed class ValidatingStatus : AbstractInstallStatus
    {
        /// <summary>
        /// 取得正在驗證的檔案名稱
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// <see cref="ValidatingStatus"/> 的建構子
        /// </summary>
        /// <param name="filename">正在驗證的檔案名稱</param>
        public ValidatingStatus(string filename)
        {
            Filename = filename;
        }
    }
}
