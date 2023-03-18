using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 簡易的下載工具類別，可自動校驗雜湊和處理 InstallTask 上的校驗失敗處理
    /// </summary>
    public sealed class DownloadHelper
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
        private readonly string filenameTemp;
        private readonly byte[] hash;
        private readonly double initPercentage, percentageMultiplier;
        private readonly bool finishInstallTaskAfterDownload, disposeWebClientAfterUsed;

        public DownloadHelper(InstallTask task, WebClient webClient, string downloadUrl, string filename, bool finishInstallTaskAfterDownload = true,
            double initPercentage = 0.0, double percentageMultiplier = 1.0, byte[] hash = null, HashMethod hashMethod = HashMethod.None,
            bool disposeWebClientAfterUsed = true)
        {
            this.task = task;
            this.webClient = webClient;
            this.downloadUrl = new Uri(downloadUrl);
            this.filename = filename;
            this.finishInstallTaskAfterDownload = finishInstallTaskAfterDownload;
            this.hash = hash;
            this.disposeWebClientAfterUsed = disposeWebClientAfterUsed;
            this.hashMethod = hashMethod;
            if (File.Exists(filename))
                filenameTemp = GetTempFileName(filename);
            else
                filenameTemp = filename;
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

        private static string GetTempFileName(string filename)
        {
            StringBuilder builder = new StringBuilder(filename, filename.Length + 5);
            builder.Append(".tmp");
            string result = builder.ToString();
            int i = -1, length = result.Length;
            while (File.Exists(result))
            {
                if (i >= 0)
                {
                    builder.Remove(length, builder.Length - length);
                }
                builder.Append((++i).ToString());
                result = builder.ToString();
            }
            return result;
        }

        public void Start()
        {
            WebClient webClient = this.webClient;
            InstallTask task = this.task;
            DownloadStatus status = this.status;
            status.Percentage = 0;
            task.ChangeStatus(status);
            task.StopRequested += StopRequestedHandler;
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            webClient.DownloadFileAsync(downloadUrl, filenameTemp);
        }

        private void StopRequestedHandler(object sender, EventArgs e)
        {
            if (webClient != null) webClient.CancelAsync();
            Dispose();
            task.StopRequested -= StopRequestedHandler;
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
                                    using (FileStream stream = File.Open(filenameTemp, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                                    using (FileStream stream = File.Open(filenameTemp, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                                    Start();
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
            if (!ReferenceEquals(filename, filenameTemp) && File.Exists(filenameTemp))
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        File.Delete(filename);
                    }
                    catch (Exception)
                    {
                    }
                }
                try
                {
                    File.Move(filenameTemp, filename);
                }
                catch (Exception)
                {
                }
            }
            if (finishInstallTaskAfterDownload)
                task.OnInstallFinished();
            else
                task.ChangeStatus(null);
            DownloadCompleted?.Invoke(this, EventArgs.Empty);
            Dispose();
        }

        private void Failed()
        {
            if (File.Exists(filenameTemp))
                File.Delete(filenameTemp);
            if (finishInstallTaskAfterDownload)
                task.OnInstallFailed();
            else
                task.ChangeStatus(null);
            DownloadFailed?.Invoke(this, EventArgs.Empty);
            Dispose();
        }

        public void Dispose()
        {
            if (disposeWebClientAfterUsed)
            {
                webClient?.Dispose();
            }
        }
    }
}
