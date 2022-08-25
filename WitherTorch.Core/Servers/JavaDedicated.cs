using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
#if NET472
using System.Net;
using System.ComponentModel;
#elif NET5_0
using System.Net.Http;
#endif
using System.Text;
using WitherTorch.Core.Utils;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// Java 原版伺服器
    /// </summary>
    public class JavaDedicated : AbstractJavaEditionServer<JavaDedicated>
    {
        protected bool _isStarted;

        protected SystemProcess process;
        private string versionString;
        private JavaRuntimeEnvironment environment;
        IPropertyFile[] propertyFiles = new IPropertyFile[1];
        public JavaPropertyFile ServerPropertiesFile => propertyFiles[0] as JavaPropertyFile;

        public JavaDedicated()
        {
            if (IsInit)
                SoftwareID = "javaDedicated";
        }

        private void InstallSoftware()
        {
            InstallTask installingTask = new InstallTask(this);
            OnInstallSoftware(installingTask);
            string manifestURL = mojangVersionInfo.ManifestURL;
            if (!string.IsNullOrEmpty(manifestURL))
            {
                bool isStop = false;
                void StopRequestedHandler(object sender, EventArgs e)
                {
                    isStop = true;
                    installingTask.StopRequested -= StopRequestedHandler;
                }
                installingTask.StopRequested += StopRequestedHandler;
#if NET472
                WebClient client = new WebClient();
                JObject jsonObject = JsonConvert.DeserializeObject<JObject>(client.DownloadString(manifestURL));
#elif NET5_0
                HttpClientHandler messageHandler = new HttpClientHandler();
                HttpClient client = new HttpClient(messageHandler);
                JObject jsonObject = JsonConvert.DeserializeObject<JObject>(client.GetStringAsync(manifestURL).Result);
#endif
                installingTask.StopRequested -= StopRequestedHandler;
                if (isStop) return;
                string downloadURL = jsonObject.GetValue("downloads")["server"]["url"].ToString();
                DownloadStatus status = new DownloadStatus(downloadURL, 0);
                installingTask.ChangeStatus(status);
#if NET472
                void StopRequestedHandler2(object sender, EventArgs e)
                {
                    if (client != null)
                    {
                        try
                        {
                            client.CancelAsync();
                            client.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    installingTask.StopRequested -= StopRequestedHandler2;
                }
                installingTask.StopRequested += StopRequestedHandler2;
                client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    installingTask.ChangePercentage(e.ProgressPercentage);
                };
                client.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
                {
                    client = null;
                    client.Dispose();
                    if (e.Error != null || e.Cancelled)
                    {
                        installingTask.OnInstallFailed();
                    }
                    else
                    {
                        installingTask.OnInstallFinished();
                    }
                    installingTask.StopRequested -= StopRequestedHandler2;
                };
                client.DownloadFileAsync(new Uri(downloadURL), Path.Combine(ServerDirectory, @"minecraft_server." + versionString + ".jar"));
#elif NET5_0
                using CancellationTokenSource source = new CancellationTokenSource();
                void StopRequestedHandler2(object sender, EventArgs e)
                {
                    try
                    {
                        source.Cancel(true);
                        client?.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                    installingTask.StopRequested -= StopRequestedHandler2;
                }
                installingTask.StopRequested += StopRequestedHandler2;
                System.Net.Http.Handlers.ProgressMessageHandler progressHandler = new System.Net.Http.Handlers.ProgressMessageHandler(messageHandler);
                progressHandler.HttpReceiveProgress += delegate (object sender, System.Net.Http.Handlers.HttpProgressEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    installingTask.ChangePercentage(e.ProgressPercentage);
                };
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await InstallUtils.HttpDownload(client, downloadURL, Path.Combine(ServerDirectory, @"minecraft_server." + versionString + ".jar"));
                        installingTask.OnInstallFinished();
                    }
                    catch (Exception)
                    {
                        installingTask.OnInstallFailed();
                    }
                    installingTask.StopRequested -= StopRequestedHandler2;
                    client.Dispose();
                    client = null;
                }, source.Token);
#endif
            }
            else
            {
                installingTask.OnInstallFailed();
            }
        }
        /// <inheritdoc/>
        public override bool ChangeVersion(int versionIndex)
        {
            try
            {
                versionString = MojangAPI.Versions[versionIndex];
                BuildVersionInfo();
                InstallSoftware();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override AbstractProcess GetProcess()
        {
            return process;
        }

        /// <inheritdoc/>
        public override string GetReadableVersion()
        {
            return versionString;
        }

        /// <inheritdoc/>
        public override IPropertyFile[] GetServerPropertyFiles()
        {
            return propertyFiles;
        }

        /// <inheritdoc/>
        public override string[] GetSoftwareVersions()
        {
            return MojangAPI.Versions;
        }

        protected override void BuildVersionInfo()
        {
            MojangAPI.VersionDictionary.TryGetValue(versionString, out mojangVersionInfo);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected override bool OnServerLoading()
        {
            try
            {
                JsonPropertyFile serverInfoJson = ServerInfoJson;
                versionString = serverInfoJson["version"].ToString();
                process = new SystemProcess();
                process.ProcessStarted += delegate (object sender, EventArgs e) { _isStarted = true; };
                process.ProcessEnded += delegate (object sender, EventArgs e) { _isStarted = false; };
                propertyFiles[0] = new JavaPropertyFile(Path.GetFullPath(Path.Combine(ServerDirectory, "./server.properties")));
                string jvmPath = (serverInfoJson["java.path"] as JValue)?.ToString() ?? null;
                string jvmPreArgs = (serverInfoJson["java.preArgs"] as JValue)?.ToString() ?? null;
                string jvmPostArgs = (serverInfoJson["java.postArgs"] as JValue)?.ToString() ?? null;
                if (jvmPath != null || jvmPreArgs != null || jvmPostArgs != null)
                {
                    environment = new JavaRuntimeEnvironment(jvmPath, jvmPreArgs, jvmPostArgs);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override RuntimeEnvironment GetRuntimeEnvironment()
        {
            return environment;
        }

        /// <inheritdoc/>
        public override void SetRuntimeEnvironment(RuntimeEnvironment environment)
        {
            if (environment is JavaRuntimeEnvironment javaRuntimeEnvironment)
                this.environment = javaRuntimeEnvironment;
            else if (environment is null)
                this.environment = null;
        }
        /// <inheritdoc/>
        public override void RunServer(RuntimeEnvironment environment)
        {
            if (!_isStarted)
            {
                if (environment is null)
                    environment = RuntimeEnvironment.JavaDefault;
                if (environment is JavaRuntimeEnvironment javaRuntimeEnvironment)
                {
                    string javaPath = javaRuntimeEnvironment.JavaPath;
                    if (javaPath is null || !File.Exists(javaPath))
                    {
                        javaPath = RuntimeEnvironment.JavaDefault.JavaPath;
                    }
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = javaPath,
                        Arguments = string.Format("-Dfile.encoding=UTF8 -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 {0} -jar \"{1}\" {2}"
                        , javaRuntimeEnvironment.JavaPreArguments ?? RuntimeEnvironment.JavaDefault.JavaPreArguments
                        , Path.Combine(ServerDirectory, @"minecraft_server." + GetReadableVersion() + ".jar")
                        , javaRuntimeEnvironment.JavaPostArguments ?? RuntimeEnvironment.JavaDefault.JavaPostArguments),
                        WorkingDirectory = ServerDirectory,
                        CreateNoWindow = true,
                        ErrorDialog = true,
                        UseShellExecute = false,
                    };
                    process.StartProcess(startInfo);
                }
            }
        }

        protected override bool OnServerSaving()
        {
            JsonPropertyFile serverInfoJson = ServerInfoJson;
            serverInfoJson["version"] = versionString;
            if (environment != null)
            {
                serverInfoJson["java.path"] = environment.JavaPath;
                serverInfoJson["java.preArgs"] = environment.JavaPreArguments;
                serverInfoJson["java.postArgs"] = environment.JavaPostArguments;
            }
            else
            {
                serverInfoJson["java.path"] = null;
                serverInfoJson["java.preArgs"] = null;
                serverInfoJson["java.postArgs"] = null;
            }
            return true;
        }

        public override bool UpdateServer()
        {
            return ChangeVersion(Array.IndexOf(MojangAPI.Versions, versionString));
        }
    }
}
