using System;

namespace WitherTorch.Core
{
    public class InstallTask
    {

        public event EventHandler InstallFinished;
        public event EventHandler InstallFailed;
        public event EventHandler PercentageChanged;
        public event EventHandler StatusChanged;
        public event EventHandler StopRequested;
        public Server Owner { get; private set; }
        public double InstallPercentage { get; private set; }
        public IInstallStatus Status { get; private set; }
        public void ChangePercentage(double percentage)
        {
            if (InstallPercentage != percentage)
            {
                InstallPercentage = percentage;
                PercentageChanged?.Invoke(this, new EventArgs());
            }
        }
        public void ChangeStatus(IInstallStatus status)
        {
            if (Status != status)
            {
                Status = status;
                OnStatusChanged();  
            }
        }
        public void ChangeStatus(string downloadURL, double percentage)
        {
            if (Status is DownloadStatus dStatus)
            {
                dStatus.DownloadFrom = downloadURL;
                dStatus.Percentage = percentage;
                OnStatusChanged();
            }
            else
            {
                ChangeStatus(new DownloadStatus(downloadURL, percentage));
            }
        }
        public void OnInstallFinished()
        {
            InstallFinished?.Invoke(this, EventArgs.Empty);
        }

        public void OnInstallFailed()
        {
            InstallFailed?.Invoke(this, EventArgs.Empty);
        }
        
        public void Stop()
        {
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        public void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public InstallTask(Server owner)
        {
            Owner = owner;
        }

    }
}
