using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
#if NET472
using System.Net;
#elif NET5_0
using System.Net.Http;
#endif

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// Bedrock 原版伺服器
    /// </summary>
    public class BedrockDedicated : Server<BedrockDedicated>
    {
        private const string manifestListURL = "https://withertorch-bds-helper.vercel.app/api/latest";
        private const string downloadURLForLinux = "https://minecraft.azureedge.net/bin-linux/bedrock-server-{0}.zip";
        private const string downloadURLForWindows = "https://minecraft.azureedge.net/bin-win/bedrock-server-{0}.zip";
        protected bool _isStarted;

        protected SystemProcess process;
        private string versionString;
        private IPropertyFile[] propertyFiles = new IPropertyFile[1];
        private static string[] versions;

        public BedrockDedicated()
        {
            if (IsInit)
            {
                SoftwareID = "bedrockDedicated";
            }
        }

        private static void LoadVersionList()
        {
#if NET472
            PlatformID platformID = Environment.OSVersion.Platform;
            using (StringReader reader = new StringReader(CachedDownloadClient.Instance.DownloadString(manifestListURL)))
            {
                bool keep = true;
                while (reader.Peek() != -1 && keep)
                {
                    string line = reader.ReadLine();
                    switch (platformID)
                    {
                        case PlatformID.Unix:
                            if (line.StartsWith("linux=") && Version.TryParse(line = line.Substring(6), out _))
                            {
                                versions = new string[] { line };
                                keep = false;
                            }
                            break;
                        case PlatformID.Win32NT:
                            if (line.StartsWith("windows=") && Version.TryParse(line = line.Substring(8), out _))
                            {
                                versions = new string[] { line };
                                keep = false;
                            }
                            break;
                    }
                }
                reader.Close();
            }
#elif NET5_0
            using (StringReader reader = new StringReader(CachedDownloadClient.Instance.DownloadString(manifestListURL)))
            {
                while (reader.Peek() != -1)
                {
                    string line = reader.ReadLine();
                    if (OperatingSystem.IsLinux())
                    {
                        if (line[..6] == "linux=" && Version.TryParse(line = line[6..], out _))
                        {
                            versions = new string[] { line };
                            break;
                        }
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        if (line[..8] == "windows=" && Version.TryParse(line = line[8..], out _))
                        {
                            versions = new string[] { line };
                            break;
                        }
                    }
                }
                reader.Close();
            }
#endif
        }

        public override bool ChangeVersion(int versionIndex)
        {
            if (versions is null) LoadVersionList();
            versionString = versions[0];
            InstallSoftware();
            return true;
        }

        public void InstallSoftware()
        {
            InstallTask installingTask = new InstallTask(this);
            OnInstallSoftware(installingTask);
#if NET472
            WebClient client = new WebClient();
#elif NET5_0
            HttpClientHandler messageHandler = new HttpClientHandler();
            HttpClient client = new HttpClient(messageHandler);
#endif
            string downloadURL;
#if NET472
            PlatformID platformID = Environment.OSVersion.Platform;
            switch (platformID)
            {
                case PlatformID.Unix:
                    downloadURL = string.Format(downloadURLForLinux, versionString);
                    break;
                case PlatformID.Win32NT:
                    downloadURL = string.Format(downloadURLForWindows, versionString);
                    break;
                default:
                    installingTask.OnInstallFailed();
                    return;
            }
#elif NET5_0
            if (OperatingSystem.IsLinux())
            {
                downloadURL = string.Format(downloadURLForLinux, versionString);
            }
            else if (OperatingSystem.IsWindows())
            {
                downloadURL = string.Format(downloadURLForWindows, versionString);
            }
            else
            {
                installingTask.OnInstallFailed();
                return;
            }
#endif
            DownloadStatus status = new DownloadStatus(downloadURL, 0);
            installingTask.ChangeStatus(status);
            StrongBox<bool> stopFlag = new StrongBox<bool>();
            void StopRequestedHandler(object sender, EventArgs e)
            {
#if NET472
               try
                {
                    client?.CancelAsync();
                }
                catch (Exception)
                {
                }
#endif
                stopFlag.Value = true;
                installingTask.StopRequested -= StopRequestedHandler;
            }
            installingTask.StopRequested += StopRequestedHandler;
#if NET472
            client.OpenReadCompleted += async delegate (object sender, OpenReadCompletedEventArgs e)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        using (ZipArchive archive = new ZipArchive(e.Result, ZipArchiveMode.Read, false))
                        {
                            System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> entries = archive.Entries;
                            System.Collections.Generic.IEnumerator<ZipArchiveEntry> enumerator = entries.GetEnumerator();
                            int currentCount = 0;
                            int count = entries.Count;
                            while (enumerator.MoveNext() && !stopFlag.Value)
                            {
                                ZipArchiveEntry entry = enumerator.Current;
                                string filePath = Path.GetFullPath(Path.Combine(ServerDirectory, entry.FullName));
                                if (filePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) // Is Directory
                                {
                                    if (!Directory.Exists(filePath))
                                        Directory.CreateDirectory(filePath);
                                }
                                else // Is File
                                {
                                    switch (entry.FullName)
                                    {
                                        case "allowlist.json":
                                        case "whitelist.json":
                                        case "permissions.json":
                                        case "server.properties":
                                            if (!File.Exists(filePath))
                                            {
                                                goto default;
                                            }
                                            break;
                                        default:
                                            {
                                                entry.ExtractToFile(filePath, true);
                                            }
                                            break;
                                    }
                                }
                                currentCount++;
                                status.Percentage = currentCount * 100.0 / count;
                                System.Threading.Tasks.Task.Run(() =>
                                {
                                    installingTask.OnStatusChanged();
                                    installingTask.ChangePercentage(status.Percentage);
                                });
                            }
                        }
                        client.Dispose();
                        installingTask.StopRequested -= StopRequestedHandler;
                        if (stopFlag.Value)
                        {
                            installingTask.OnInstallFailed();
                        }
                        else
                        {
                            installingTask.ChangePercentage(100);
                            installingTask.OnInstallFinished();
                        }
                    }
                    catch (Exception)
                    {
                        installingTask.StopRequested -= StopRequestedHandler;
                        installingTask.OnInstallFailed();
                    }
                });
            };
            client.OpenReadAsync(new Uri(downloadURL));
#elif NET5_0
            System.Net.Http.Handlers.ProgressMessageHandler progressHandler = new System.Net.Http.Handlers.ProgressMessageHandler(messageHandler);
            progressHandler.HttpReceiveProgress += delegate (object sender, System.Net.Http.Handlers.HttpProgressEventArgs e)
            {
                status.Percentage = e.ProgressPercentage;
                installingTask.OnStatusChanged();
                installingTask.ChangePercentage(e.ProgressPercentage * 0.65);
            };
            Task.Run(async () =>
            {
                try
                {
                    using (Stream dataStream = await client.GetStreamAsync(new Uri(downloadURL)))
                    using (ZipArchive archive = new ZipArchive(dataStream, ZipArchiveMode.Read, false))
                    {
                        installingTask.ChangePercentage(65);
                        DecompessionStatus decompessionStatus = new DecompessionStatus();
                        installingTask.ChangeStatus(decompessionStatus);
                        System.Collections.ObjectModel.ReadOnlyCollection<ZipArchiveEntry> entries = archive.Entries;
                        System.Collections.Generic.IEnumerator<ZipArchiveEntry> enumerator = entries.GetEnumerator();
                        int currentCount = 0;
                        int count = entries.Count;
                        while (enumerator.MoveNext() && !stopFlag.Value)
                        {
                            ZipArchiveEntry entry = enumerator.Current;
                            string filePath = Path.GetFullPath(Path.Combine(ServerDirectory, entry.FullName));
                            if (filePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)) // Is Directory
                            {
                                if (!Directory.Exists(filePath))
                                    Directory.CreateDirectory(filePath);
                            }
                            else // Is File
                            {
                                switch (entry.FullName)
                                {
                                    case "allowlist.json":
                                    case "whitelist.json":
                                    case "permissions.json":
                                    case "server.properties":
                                        if (!File.Exists(filePath))
                                        {
                                            goto default;
                                        }
                                        break;
                                    default:
                                        {
                                            entry.ExtractToFile(filePath, true);
                                        }
                                        break;
                                }
                            }
                            currentCount++;
                            status.Percentage = currentCount * 100.0 / count;
                            installingTask.OnStatusChanged();
                            installingTask.ChangePercentage(65 + decompessionStatus.Percentage * 0.35);
                        }
                        dataStream.Close();
                    }
                    client.Dispose();
                    installingTask.StopRequested -= StopRequestedHandler;
                    if (stopFlag.Value)
                    {
                        installingTask.OnInstallFailed();
                    }
                    else
                    {
                        installingTask.ChangePercentage(100);
                        installingTask.OnInstallFinished();
                    }
                }
                catch (Exception)
                {
                    installingTask.OnInstallFailed();
                }
                installingTask.StopRequested -= StopRequestedHandler;
            });
#endif
        }

        public override AbstractProcess GetProcess()
        {
            return process;
        }

        public override string GetReadableVersion()
        {
            return versionString;
        }

        public override RuntimeEnvironment GetRuntimeEnvironment()
        {
            return null;
        }

        public override IPropertyFile[] GetServerPropertyFiles()
        {
            return propertyFiles;
        }

        public override string[] GetSoftwareVersions()
        {
            if (versions is null) LoadVersionList();
            return versions;
        }

        public override void RunServer(RuntimeEnvironment environment)
        {
            if (!_isStarted)
            {
                string path = Path.Combine(ServerDirectory, "bedrock_server.exe");
                if (File.Exists(path))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        WorkingDirectory = ServerDirectory,
                        CreateNoWindow = true,
                        ErrorDialog = true,
                        UseShellExecute = false,
                    };
                    process.StartProcess(startInfo);
                }
            }
        }

        /// <inheritdoc/>
        public override void StopServer(bool force)
        {
            if (_isStarted)
            {
                if (force)
                {
                    process.Kill();
                }
                else
                {
                    process.InputCommand("stop");
                }
            }
        }

        public override void SetRuntimeEnvironment(RuntimeEnvironment environment)
        {
        }

        public override bool UpdateServer()
        {
            return ChangeVersion(default);
        }

        protected override bool CreateServer()
        {
            try
            {
                process = new SystemProcess();
                process.ProcessStarted += delegate (object sender, EventArgs e) { _isStarted = true; };
                process.ProcessEnded += delegate (object sender, EventArgs e) { _isStarted = false; };
                propertyFiles[0] = new JavaPropertyFile(Path.Combine(ServerDirectory, "./server.properties"));
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        protected override bool OnServerLoading()
        {
            try
            {
                JsonPropertyFile serverInfoJson = ServerInfoJson;
                versionString = serverInfoJson["version"].ToString();
                process = new SystemProcess();
                process.ProcessStarted += delegate (object sender, EventArgs e) { _isStarted = true; };
                process.ProcessEnded += delegate (object sender, EventArgs e) { _isStarted = false; };
                propertyFiles[0] = new JavaPropertyFile(Path.Combine(ServerDirectory, "./server.properties"));
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        protected override bool OnServerSaving()
        {
            JsonPropertyFile serverInfoJson = ServerInfoJson;
            serverInfoJson["version"] = versionString;
            return true;
        }
    }
}
