using System;
using System.Diagnostics;
using System.IO;
#if NET472
using System.ComponentModel;
using System.Net;
#elif NET5_0
using System.Threading.Tasks;
#endif
using System.Text;
using System.Xml;
using System.Runtime.CompilerServices;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 操作 Spigot 官方的建置工具 (BuildTools) 的類別，此類別無法被繼承
    /// </summary>
    public sealed class FabricInstaller
    {
        private const string manifestListURL = "https://maven.fabricmc.net/net/fabricmc/fabric-installer/maven-metadata.xml";
        private const string downloadURL = "https://maven.fabricmc.net/net/fabricmc/fabric-installer/{0}/fabric-installer-{0}.jar";
        private readonly static DirectoryInfo workingDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, WTCore.FabricInstallerPath));
        private readonly static FileInfo buildToolFileInfo = new FileInfo(Path.Combine(workingDirectoryInfo.FullName + "./fabric-installer.jar"));
        private readonly static FileInfo buildToolVersionInfo = new FileInfo(Path.Combine(workingDirectoryInfo.FullName + "./fabric-installer.version"));
        private event EventHandler UpdateStarted;
        private event UpdateProgressEventHandler UpdateProgressChanged;
        private event EventHandler UpdateFinished;
        private static FabricInstaller _instance;
        public static FabricInstaller Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FabricInstaller();
                return _instance;
            }
        }
        public enum BuildTarget
        {
            CraftBukkit,
            Spigot
        }
        private bool CheckUpdate(out string updatedVersion)
        {
            string version = null;
            string nowVersion = null;
            if (workingDirectoryInfo.Exists)
            {
                if (buildToolVersionInfo.Exists && buildToolFileInfo.Exists)
                {
                    using (StreamReader reader = buildToolVersionInfo.OpenText())
                    {
                        string versionText;
                        do
                        {
                            versionText = reader.ReadLine();
                        } while (string.IsNullOrWhiteSpace(versionText));
                        if (!string.IsNullOrWhiteSpace(versionText)) version = versionText;
                    }
                }
            }
            else
            {
                workingDirectoryInfo.Create();
            }

            XmlDocument manifestXML = new XmlDocument();
#if NET472
            using (WebClient client = new WebClient() { Encoding = Encoding.UTF8 })
            {
                client.Headers.Set(HttpRequestHeader.UserAgent, @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36");
                manifestXML.LoadXml(client.DownloadString(manifestListURL));
            }
#elif NET5_0
                using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                {
                    manifestXML.LoadXml(client.GetStringAsync(manifestListURL).Result);
                }
#endif
            nowVersion = manifestXML.SelectSingleNode("//metadata/versioning/latest").InnerText;
            if (version != nowVersion)
            {
                version = null;
            }
            updatedVersion = nowVersion;
            return version == null;
        }
        private void Update(InstallTask installTask, string version)
        {
            UpdateStarted?.Invoke(this, EventArgs.Empty);
#if NET472
            WebClient client = new WebClient();
            void StopRequestedHandler(object sender, EventArgs e)
            {
                try
                {
                    client?.CancelAsync();
                    client?.Dispose();
                }
                catch (Exception)
                {
                }
                installTask.StopRequested -= StopRequestedHandler;
            };
            installTask.StopRequested += StopRequestedHandler;
            client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                        {
                            UpdateProgressChanged?.Invoke(e.ProgressPercentage);
                        };
            client.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
            {
                client.Dispose();
                client = null;
                using (StreamWriter writer = buildToolVersionInfo.CreateText())
                {
                    writer.WriteLine(version);
                    writer.Flush();
                    writer.Close();
                }
                installTask.StopRequested -= StopRequestedHandler;
                UpdateFinished?.Invoke(this, EventArgs.Empty);
            };
            client.Headers.Set(HttpRequestHeader.UserAgent, @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36");
            client.DownloadFileAsync(new Uri(string.Format(downloadURL, version)), buildToolFileInfo.FullName);
#elif NET5_0
            System.Net.Http.HttpClientHandler messageHandler = new System.Net.Http.HttpClientHandler();
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(messageHandler);
            System.Net.Http.Handlers.ProgressMessageHandler progressHandler = new System.Net.Http.Handlers.ProgressMessageHandler(messageHandler);
            progressHandler.HttpReceiveProgress += delegate (object sender, System.Net.Http.Handlers.HttpProgressEventArgs e)
            {
                UpdateProgressChanged?.Invoke(e.ProgressPercentage);
            };
            StrongBox<bool> stopFlag = new StrongBox<bool>();
            void StopRequestedHandler(object sender, EventArgs e)
            {
                stopFlag.Value = true;
                installTask.StopRequested -= StopRequestedHandler;
            }
            installTask.StopRequested += StopRequestedHandler;
            Task.Run(async () =>
            {
                using Stream dataStream = await client.GetStreamAsync(new Uri(string.Format(downloadURL, version)));
                using FileStream fileStream = new FileStream(buildToolFileInfo.FullName, FileMode.Create);
                byte[] buffer = new byte[InstallUtils.BUFFER_SIZE];
                int length;
                while ((length = dataStream.Read(buffer, 0, InstallUtils.BUFFER_SIZE)) > 0 && !stopFlag.Value)
                {
                    fileStream.Write(buffer, 0, length);
                    fileStream.Flush();
                }
                dataStream.Close();
                fileStream.Close();
                client.Dispose();
                installTask.StopRequested -= StopRequestedHandler;
                if (!stopFlag.Value)
                {
                    using (StreamWriter writer = buildToolVersionInfo.CreateText())
                    {
                        writer.WriteLine(version);
                        writer.Flush();
                        writer.Close();
                    }
                    UpdateFinished?.Invoke(this, EventArgs.Empty);
                }
            });
#endif
        }
        public delegate void UpdateProgressEventHandler(int progress);

        public void Install(InstallTask task, string version)
        {
            InstallTask installTask = task;
            FabricInstallerStatus status = new FabricInstallerStatus(SpigotBuildToolsStatus.ToolState.Initialize, 0);
            installTask.ChangeStatus(status);
            bool isStop = false;
            void StopRequestedHandler(object sender, EventArgs e)
            {
                isStop = true;
                installTask.StopRequested -= StopRequestedHandler;
            };
            installTask.StopRequested += StopRequestedHandler;
            bool hasUpdate = CheckUpdate(out string newVersion);
            installTask.StopRequested -= StopRequestedHandler;
            if (isStop) return;
            if (hasUpdate)
            {
                UpdateStarted += (sender, e) =>
                {
                    status.State = SpigotBuildToolsStatus.ToolState.Update;
                    installTask.OnStatusChanged();
                };
                UpdateProgressChanged += (progress) =>
                {
                    status.Percentage = progress;
                    installTask.ChangePercentage(progress / 2);
                    installTask.OnStatusChanged();
                };
                UpdateFinished += (sender, e) =>
                {
                    installTask.ChangePercentage(50);
                    installTask.OnStatusChanged();
                    DoInstall(installTask, status, version);
                };
                Update(installTask, newVersion);
            }
            else
            {
                installTask.ChangePercentage(50);
                installTask.OnStatusChanged();
                DoInstall(installTask, status, version);
            }
        }

        public void Install(InstallTask task, string minecraftVersion, string fabricVersion)
        {
            InstallTask installTask = task;
            FabricInstallerStatus status = new FabricInstallerStatus(SpigotBuildToolsStatus.ToolState.Initialize, 0);
            installTask.ChangeStatus(status);
            bool isStop = false;
            void StopRequestedHandler(object sender, EventArgs e)
            {
                isStop = true;
                installTask.StopRequested -= StopRequestedHandler;
            };
            installTask.StopRequested += StopRequestedHandler;
            bool hasUpdate = CheckUpdate(out string newVersion);
            installTask.StopRequested -= StopRequestedHandler;
            if (isStop) return;
            if (hasUpdate)
            {
                UpdateStarted += (sender, e) =>
                {
                    status.State = SpigotBuildToolsStatus.ToolState.Update;
                    installTask.OnStatusChanged();
                };
                UpdateProgressChanged += (progress) =>
                {
                    status.Percentage = progress;
                    installTask.ChangePercentage(progress / 2);
                    installTask.OnStatusChanged();
                };
                UpdateFinished += (sender, e) =>
                {
                    installTask.ChangePercentage(50);
                    installTask.OnStatusChanged();
                    DoInstall(installTask, status, minecraftVersion, fabricVersion);
                };
                Update(installTask, newVersion);
            }
            else
            {
                installTask.ChangePercentage(50);
                installTask.OnStatusChanged();
                DoInstall(installTask, status, minecraftVersion, fabricVersion);
            }
        }

        private void DoInstall(in InstallTask task, in FabricInstallerStatus status, string version)
        {
            InstallTask installTask = task;
            FabricInstallerStatus installStatus = status;
            installStatus.State = SpigotBuildToolsStatus.ToolState.Build;
            installTask.OnStatusChanged();
            JavaRuntimeEnvironment environment = RuntimeEnvironment.JavaDefault;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = environment.JavaPath,
                Arguments = string.Format("-Xms512M -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 -jar \"{0}\" server -mcversion {1} -dir \"{2}\" -downloadMinecraft", buildToolFileInfo.FullName, version, installTask.Owner.ServerDirectory),
                WorkingDirectory = workingDirectoryInfo.FullName,
                CreateNoWindow = true,
                ErrorDialog = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            System.Diagnostics.Process innerProcess = System.Diagnostics.Process.Start(startInfo);
            innerProcess.EnableRaisingEvents = true;
            innerProcess.BeginOutputReadLine();
            innerProcess.BeginErrorReadLine();
            void StopRequestedHandler(object sender, EventArgs e)
            {
                try
                {
                    innerProcess.Kill();
                }
                catch (Exception)
                {
                }
                installTask.StopRequested -= StopRequestedHandler;
            };
            installTask.StopRequested += StopRequestedHandler;
            innerProcess.OutputDataReceived += (sender, e) =>
            {
                installStatus.OnProcessMessageReceived(sender, e);
            };
            innerProcess.ErrorDataReceived += (sender, e) =>
            {
                installStatus.OnProcessMessageReceived(sender, e);
            };
            innerProcess.Exited += (sender, e) =>
            {
                installTask.StopRequested -= StopRequestedHandler;
                installTask.OnInstallFinished();
                installTask.ChangePercentage(100);
                innerProcess.Dispose();
            };
        }

        private void DoInstall(in InstallTask task, in FabricInstallerStatus status, string minecraftVersion, string fabricVersion)
        {
            InstallTask installTask = task;
            FabricInstallerStatus installStatus = status;
            installStatus.State = SpigotBuildToolsStatus.ToolState.Build;
            installTask.OnStatusChanged();
            JavaRuntimeEnvironment environment = RuntimeEnvironment.JavaDefault;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = environment.JavaPath,
                Arguments = string.Format("-Xms512M -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 -jar \"{0}\" server -mcversion {1} -loader {2} -dir \"{3}\" -downloadMinecraft", buildToolFileInfo.FullName, minecraftVersion, fabricVersion, installTask.Owner.ServerDirectory),
                WorkingDirectory = workingDirectoryInfo.FullName,
                CreateNoWindow = true,
                ErrorDialog = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            System.Diagnostics.Process innerProcess = System.Diagnostics.Process.Start(startInfo);
            innerProcess.EnableRaisingEvents = true;
            innerProcess.BeginOutputReadLine();
            innerProcess.BeginErrorReadLine();
            innerProcess.OutputDataReceived += (sender, e) =>
            {
                installStatus.OnProcessMessageReceived(sender, e);
            };
            innerProcess.ErrorDataReceived += (sender, e) =>
            {
                installStatus.OnProcessMessageReceived(sender, e);
            };
            innerProcess.Exited += (sender, e) =>
            {
                installTask.OnInstallFinished();
                installTask.ChangePercentage(100);
                innerProcess.Dispose();
            };
        }
    }
}
