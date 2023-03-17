using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace WitherTorch.Core.Utils
{
    internal sealed class DownloadHelper
    {
        public enum HashMethod
        {
            None = 0,
            Sha1 = 1,
            Sha256 = 2
        }

        public event EventHandler DownloadCompleted;
        public event EventHandler DownloadFailed;

        private readonly InstallTask task;
        private readonly WebClient webClient;
        private readonly Uri downloadUrl;
        private readonly DownloadStatus status;
        private readonly HashMethod hashMethod;
        private readonly string filename;
        private readonly byte[] hash;
        private readonly double initPercentage, percentageMultiplier;
        private readonly bool finishTaskAfterDownload, disposeWebClientAfterUsed;
        private bool disposedValue;

        public DownloadHelper(InstallTask task, WebClient webClient, string downloadUrl, string filename, bool finishTaskAfterDownload = true,
            double initPercentage = 0.0, double percentageMultiplier = 1.0, byte[] hash = null, HashMethod hashMethod = HashMethod.None,
            bool disposeWebClientAfterUsed = true)
        {
            this.task = task;
            this.webClient = webClient;
            this.downloadUrl = new Uri(downloadUrl);
            this.filename = filename;
            this.finishTaskAfterDownload = finishTaskAfterDownload;
            this.hash = hash;
            this.disposeWebClientAfterUsed = disposeWebClientAfterUsed;
            this.hashMethod = hashMethod;
            double maxMultiplier;
            if (initPercentage <= 0)
            {
                this.initPercentage = 0.0;
                maxMultiplier = 1.0;
            }
            else
            {
                double percentage = initPercentage > 100 ? 100 : initPercentage;
                this.initPercentage = percentage;
                maxMultiplier = (100 - initPercentage) / 100.0;
            }
            this.percentageMultiplier = percentageMultiplier > maxMultiplier ? maxMultiplier : (percentageMultiplier < 0 ? 0 : percentageMultiplier);
            status = new DownloadStatus(downloadUrl);
        }

        public void StopRequestedHandler(object sender, EventArgs e)
        {
            if (webClient != null) webClient.CancelAsync();
            Dispose();
            task.StopRequested -= StopRequestedHandler;
        }

        public void StartDownload()
        {
            WebClient webClient = this.webClient;
            InstallTask task = this.task;
            DownloadStatus status = this.status;
            status.Percentage = 0;
            task.ChangeStatus(status);
            task.StopRequested += StopRequestedHandler;
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            webClient.DownloadFileAsync(downloadUrl, filename);
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double percentage, initPercentage = this.initPercentage, percentageMultiplier = this.percentageMultiplier;
            percentage = e.ProgressPercentage;
            status.Percentage = percentage;
            if (percentageMultiplier < 1.0)
                percentage *= percentageMultiplier;
            if (initPercentage > 0)
                task.ChangePercentage(initPercentage + percentage);
            else
                task.ChangePercentage(percentage);
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            task.StopRequested -= StopRequestedHandler;
            webClient.DownloadProgressChanged -= WebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted -= WebClient_DownloadFileCompleted;
            if (!e.Cancelled)
            {
                if (e.Error is null)
                {
                    byte[] exceptedHash = hash;
                    if (exceptedHash is object) //SHA-1 校驗
                    {
                        byte[] actualHash;
                        switch (hashMethod)
                        {
                            case HashMethod.Sha1:
                                task.ChangeStatus(new ValidatingStatus(filename));
                                try
                                {
                                    using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                                        actualHash = HashHelper.ComputeSha1Hash(stream);
                                }
                                catch (Exception)
                                {
                                    actualHash = null;
                                }
                                break;
                            case HashMethod.Sha256:
                                task.ChangeStatus(new ValidatingStatus(filename));
                                try
                                {
                                    using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                                        actualHash = HashHelper.ComputeSha256Hash(stream);
                                }
                                catch (Exception)
                                {
                                    actualHash = null;
                                }
                                break;
                            default:
                                actualHash = exceptedHash;
                                break;
                        }
                        if (HashHelper.ByteArrayEquals(actualHash, exceptedHash))
                        {
                            Finished();
                        }
                        else
                        {
                            switch (task.OnValidateFailed(filename, actualHash, exceptedHash))
                            {
                                case InstallTask.ValidateFailedState.Cancel:
                                    Finished();
                                    break;
                                case InstallTask.ValidateFailedState.Ignore:
                                    Failed();
                                    break;
                                case InstallTask.ValidateFailedState.Retry:
                                    StartDownload();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Finished();
                    }
                }
                else
                {
                    Failed();
                }
            }
        }

        private void Finished()
        {
            if (finishTaskAfterDownload)
                task.OnInstallFinished();
            else
                task.ChangeStatus(null);
            DownloadCompleted?.Invoke(this, EventArgs.Empty);
            Dispose();
        }

        private void Failed()
        {
            if (finishTaskAfterDownload)
                task.OnInstallFailed();
            else
                task.ChangeStatus(null);
            DownloadFailed?.Invoke(this, EventArgs.Empty);
            Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                    if (disposeWebClientAfterUsed)
                    {
                        webClient.Dispose();
                    }
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        // ~DownloadHelper()
        // {
        //     // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
