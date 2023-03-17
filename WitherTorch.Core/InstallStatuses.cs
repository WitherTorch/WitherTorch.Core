using System;
using System.Diagnostics;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個安裝狀態。
    /// </summary>
    public abstract class AbstractInstallStatus
    {
        /// <summary>
        /// 當狀態物件的內容改變時觸發
        /// </summary>
        public event EventHandler Updated;

        protected void OnUpdated()
        {
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ProcessStatus : AbstractInstallStatus
    {
        public event DataReceivedEventHandler ProcessMessageReceived;

        private double _percentage;
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

        public ProcessStatus(double percentage)
        {
            _percentage = percentage;
        }

        public virtual void OnProcessMessageReceived(object sender, DataReceivedEventArgs e)
        {
            ProcessMessageReceived?.Invoke(sender, e);
        }
    }

    public class DownloadStatus : AbstractInstallStatus
    {
        public string DownloadFrom { get; }

        private double _percentage;
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

        public DownloadStatus(string from, double percentage = 0.0)
        {
            DownloadFrom = from;
            Percentage = percentage;
        }
    }

    public class SpigotBuildToolsStatus : ProcessStatus
    {
        public enum ToolState
        {
            Initialize,
            Update,
            Build
        }

        private ToolState _state;

        public ToolState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                OnUpdated();
            }
        }

        public SpigotBuildToolsStatus(ToolState state, double percentage) : base(percentage)
        {
            State = state;
        }
    }

    public class FabricInstallerStatus : SpigotBuildToolsStatus
    {
        public FabricInstallerStatus(ToolState state, double percentage) : base(state, percentage)
        {
        }
    }

    public class DecompessionStatus : AbstractInstallStatus
    {
        private double _percentage;
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

        public DecompessionStatus(double percentage = 0.0)
        {
            Percentage = percentage;
        }
    }
}
