using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
#if NET472
using System.ComponentModel;
using System.Net;
#elif NET5_0
using System.Threading.Tasks;
#endif
using System.Xml;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 操作 Spigot 官方的建置工具 (BuildTools) 的類別，此類別無法被繼承
    /// </summary>
    public sealed class SpigotBuildTools
    {
        private const string manifestListURL = "https://hub.spigotmc.org/jenkins/job/BuildTools/api/xml";
        private const string downloadURL = "https://hub.spigotmc.org/jenkins/job/BuildTools/lastSuccessfulBuild/artifact/target/BuildTools.jar";
        private readonly static DirectoryInfo workingDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, WTCore.SpigotBuildToolsPath));
        private readonly static FileInfo buildToolFileInfo = new FileInfo(Path.Combine(workingDirectoryInfo.FullName + "./BuildTools.jar"));
        private readonly static FileInfo buildToolVersionInfo = new FileInfo(Path.Combine(workingDirectoryInfo.FullName + "./BuildTools.version"));
        private event EventHandler UpdateStarted;
        private event UpdateProgressEventHandler UpdateProgressChanged;
        private event EventHandler UpdateFinished;
        private static SpigotBuildTools _instance;
        public static SpigotBuildTools Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SpigotBuildTools();
                return _instance;
            }
        }
        public enum BuildTarget
        {
            CraftBukkit,
            Spigot
        }
        private bool CheckUpdate(out int updatedVersion)
        {
            int version = -1;
            int nowVersion = -1;
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
                        } while (!int.TryParse(versionText, out version));
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
            nowVersion = int.Parse(manifestXML.SelectSingleNode("//mavenModuleSet/lastSuccessfulBuild/number").InnerText);
            if (version < nowVersion)
            {
                version = -1;
            }
            updatedVersion = nowVersion;
            return version <= 0;
        }
        private void Update(InstallTask installTask, int version)
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
                    writer.WriteLine(version.ToString());
                    writer.Flush();
                    writer.Close();
                }
                installTask.StopRequested -= StopRequestedHandler;
                UpdateFinished?.Invoke(this, EventArgs.Empty);
            };
            client.Headers.Set(HttpRequestHeader.UserAgent, @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36");
            client.DownloadFileAsync(new Uri(downloadURL), buildToolFileInfo.FullName);
#elif NET5_0
            System.Net.Http.HttpClientHandler messageHandler = new System.Net.Http.HttpClientHandler();
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(messageHandler);
            using CancellationTokenSource source = new CancellationTokenSource();
            void StopRequestedHandler(object sender, EventArgs e)
            {
                try
                {
                    source.Cancel(true);
                    client?.Dispose();
                }
                catch (Exception)
                {
                }
                installTask.StopRequested -= StopRequestedHandler;
            };
            installTask.StopRequested += StopRequestedHandler;
            System.Net.Http.Handlers.ProgressMessageHandler progressHandler = new System.Net.Http.Handlers.ProgressMessageHandler(messageHandler);
            progressHandler.HttpReceiveProgress += delegate (object sender, System.Net.Http.Handlers.HttpProgressEventArgs e)
            {
                UpdateProgressChanged?.Invoke(e.ProgressPercentage);
            };
            Task.Run(async () =>
            {
                Stream dataStream = await client.GetStreamAsync(new Uri(downloadURL));
                FileStream fileStream = new FileStream(buildToolFileInfo.FullName, FileMode.Create);
                byte[] buffer = new byte[1 << 20];
                while (true)
                {
                    int length = dataStream.Read(buffer, 0, buffer.Length);
                    if (length > 0)
                    {
                        fileStream.Write(buffer, 0, length);
                    }
                    else
                    {
                        break;
                    }
                }
                dataStream.Close();
                fileStream.Close();
                await dataStream.DisposeAsync();
                await fileStream.DisposeAsync();
                client.Dispose();
                using (StreamWriter writer = buildToolVersionInfo.CreateText())
                {
                    writer.WriteLine(version.ToString());
                    writer.Flush();
                    writer.Close();
                }
                installTask.StopRequested -= StopRequestedHandler;
                UpdateFinished?.Invoke(this, EventArgs.Empty);
            }, source.Token);
#endif
        }
        public delegate void UpdateProgressEventHandler(int progress);

        public void Install(InstallTask task, BuildTarget target, string version)
        {
            InstallTask installTask = task;
            SpigotBuildToolsStatus status = new SpigotBuildToolsStatus(SpigotBuildToolsStatus.ToolState.Initialize, 0);
            installTask.ChangeStatus(status);
            bool isStop = false;
            void StopRequestedHandler(object sender, EventArgs e)
            {
                isStop = true;
                installTask.StopRequested -= StopRequestedHandler;
            };
            installTask.StopRequested += StopRequestedHandler;
            bool hasUpdate = CheckUpdate(out int newVersion);
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
                    DoInstall(installTask, status, target, version);
                };
                Update(installTask, newVersion);
            }
            else
            {
                installTask.ChangePercentage(50);
                installTask.OnStatusChanged();
                DoInstall(installTask, status, target, version);
            }
        }

        private void DoInstall(InstallTask task, SpigotBuildToolsStatus status, BuildTarget target, string version)
        {
            InstallTask installTask = task;
            SpigotBuildToolsStatus installStatus = status;
            installStatus.State = SpigotBuildToolsStatus.ToolState.Build;
            installTask.OnStatusChanged();
            JavaRuntimeEnvironment environment = RuntimeEnvironment.JavaDefault;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = environment.JavaPath,
                Arguments = string.Format("-Xms512M -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 -jar \"{0}\" --rev {1} --compile {2} --output-dir \"{3}\"", buildToolFileInfo.FullName, version, target.ToString().ToLower(), installTask.Owner.ServerDirectory),
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
    }
}
