namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個安裝狀態。此介面只作為分類之用，無實際功能
    /// </summary>
    public interface IInstallStatus
    {
        //This interface is empty, just use for catagory
    }

    public class ProcessStatus : IInstallStatus
    {
        public event System.Diagnostics.DataReceivedEventHandler ProcessMessageReceived;
        public double Percentage { get; set; }
        public ProcessStatus(double percentage)
        {
            Percentage = percentage;
        }
        public virtual void OnProcessMessageReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            ProcessMessageReceived?.Invoke(sender, e);
        }
    }

    public class DownloadStatus : IInstallStatus
    {
        public string DownloadFrom { get; set; }
        public double Percentage { get; set; }
        public DownloadStatus(string from, double percentage)
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
        public ToolState State { get; set; }

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

    public class DecompessionStatus : IInstallStatus
    {
        public double Percentage { get; set; }
    }
}
