using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#if NET472
using System.ComponentModel;
using System.Net;
#elif NET5_0
using System.Net.Http;
#endif
using System.Text;
using WitherTorch.Core.Utils;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// Paper 伺服器
    /// </summary>
    public class Paper : AbstractJavaEditionServer<Paper>
    {
        private const string manifestListURL = "https://api.papermc.io/v2/projects/paper";
        private const string manifestListURL2 = "https://api.papermc.io/v2/projects/paper/versions/{0}";
        private const string downloadURL = "https://api.papermc.io/v2/projects/paper/versions/{0}/builds/{1}/downloads/paper-{0}-{1}.jar";

        protected bool _isStarted;
        IPropertyFile[] propertyFiles = new IPropertyFile[4];
        public JavaPropertyFile ServerPropertiesFile => propertyFiles[0] as JavaPropertyFile;
        public YamlPropertyFile BukkitYMLFile => propertyFiles[1] as YamlPropertyFile;
        public YamlPropertyFile SpigotYMLFile => propertyFiles[2] as YamlPropertyFile;
        public YamlPropertyFile PaperYMLFile => propertyFiles[3] as YamlPropertyFile;
        private string versionString;
        private long build = -1;
        private JavaRuntimeEnvironment environment;
        protected SystemProcess process;
        internal static string[] versions;

        public Paper()
        {
            if (IsInInit)
            {
                SoftwareRegistrationDelegate += Initialize;
                SoftwareID = "paper";
            }
        }

        // 註冊時會執行這個函式
        private static void Initialize()
        {
            if (versions == null)
            {
                LoadVersionList();
            }
        }

        internal static void LoadVersionList()
        {
            List<string> preparingVersionList = new List<string>();
            try
            {
                string manifestString = CachedDownloadClient.Instance.DownloadString(manifestListURL);
                if (manifestString != null)
                {
                    JObject manifestJSON = JsonConvert.DeserializeObject<JObject>(manifestString);
                    if (manifestJSON != null)
                    {
                        JArray versionArray = manifestJSON.GetValue("versions") as JArray;
                        if (versionArray != null)
                        {
                            foreach (JToken token in versionArray)
                            {
                                if (token is JValue tokenValue && tokenValue.Type == JTokenType.String)
                                {
                                    string version = tokenValue.Value.ToString();
                                    preparingVersionList.Add(version);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            preparingVersionList.Reverse();
            versions = preparingVersionList.ToArray();
        }

        private void InstallSoftware()
        {
            JObject manifestJSON2 = null;
#if NET472
            WebClient client = new WebClient();
            manifestJSON2 = JsonConvert.DeserializeObject<JObject>(client.DownloadString(string.Format(manifestListURL2, versionString)));
#elif NET5_0
            HttpClientHandler messageHandler = new HttpClientHandler();
            HttpClient client = new HttpClient(messageHandler);
            manifestJSON2 = JsonConvert.DeserializeObject<JObject>(client.GetStringAsync(string.Format(manifestListURL2, versionString)).Result);
#endif
            JArray buildArray = manifestJSON2.GetValue("builds") as JArray;
            if (buildArray != null && buildArray.Last is JValue rawBuildValue && rawBuildValue.Value is long build)
            {
                this.build = build;
                string downloadURL = string.Format(Paper.downloadURL, versionString, build);
                InstallTask installingTask = new InstallTask(this);
                DownloadStatus status = new DownloadStatus(downloadURL, 0);
                installingTask.ChangeStatus(status);
#if NET472
                client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    installingTask.ChangePercentage(e.ProgressPercentage);
                };
                client.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
                {
                    client.Dispose();
                    installingTask.OnInstallFinished();
                };
                client.DownloadFileAsync(new Uri(downloadURL), Path.Combine(ServerDirectory, @"paper-" + versionString + ".jar"));
                OnInstallSoftware(installingTask);
#elif NET5_0
                System.Net.Http.Handlers.ProgressMessageHandler progressHandler = new System.Net.Http.Handlers.ProgressMessageHandler(messageHandler);
                progressHandler.HttpReceiveProgress += delegate (object sender, System.Net.Http.Handlers.HttpProgressEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    installingTask.ChangePercentage(e.ProgressPercentage);
                };
                OnInstallSoftware(installingTask);
                System.Threading.Tasks.Task.Run(async () =>
                {
                    Stream dataStream = await client.GetStreamAsync(new Uri(downloadURL));
                    FileStream fileStream = new FileStream(System.IO.Path.Combine(ServerDirectory, @"paper-" + versionString + ".jar"), FileMode.Create);
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
                    installingTask.OnInstallFinished();
                });
#endif
            }
        }

        public override bool ChangeVersion(int versionIndex)
        {
            try
            {
                versionString = versions[versionIndex];
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
            return versions;
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
                propertyFiles[1] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./bukkit.yml"));
                propertyFiles[2] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./spigot.yml"));
                propertyFiles[3] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./paper.yml"));
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
                JToken buildNode = serverInfoJson["build"];
                if (buildNode?.Type == JTokenType.Integer)
                {
                    build = (long)buildNode;
                }
                else
                {
                    build = 0L;
                }
                process = new SystemProcess();
                process.ProcessStarted += delegate (object sender, EventArgs e) { _isStarted = true; };
                process.ProcessEnded += delegate (object sender, EventArgs e) { _isStarted = false; };
                propertyFiles[0] = new JavaPropertyFile(Path.Combine(ServerDirectory, "./server.properties"));
                propertyFiles[1] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./bukkit.yml"));
                propertyFiles[2] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./spigot.yml"));
                propertyFiles[3] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./paper.yml"));
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
        public override void SetRuntimeEnvironment(RuntimeEnvironment environment)
        {
            if (environment is JavaRuntimeEnvironment javaRuntimeEnvironment)
                this.environment = javaRuntimeEnvironment;
            else if (environment is null)
                this.environment = null;
        }

        /// <inheritdoc/>
        public override RuntimeEnvironment GetRuntimeEnvironment()
        {
            return environment;
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
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = javaRuntimeEnvironment.JavaPath ?? RuntimeEnvironment.JavaDefault.JavaPath,
                        Arguments = string.Format("-Djline.terminal=jline.UnsupportedTerminal -Dfile.encoding=UTF8 -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 {0} -jar \"{1}\" {2}"
                        , javaRuntimeEnvironment.JavaPreArguments ?? RuntimeEnvironment.JavaDefault.JavaPreArguments
                        , Path.Combine(ServerDirectory, @"paper-" + GetReadableVersion() + ".jar")
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
            serverInfoJson["build"] = build;
            if (environment != null)
            {
                serverInfoJson["java.path"] = environment.JavaPath;
                serverInfoJson["java.preArgs"] = environment.JavaPreArguments;
                serverInfoJson["java.postArgs"] = environment.JavaPostArguments;
            }
            return true;
        }

        public override void UpdateServer()
        {
            ChangeVersion(Array.IndexOf(versions, versionString));
        }
    }
}
