using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using WitherTorch.Core.Utils;
using System.Runtime.CompilerServices;
#if NET472
using System.Net;
using System.ComponentModel;
#elif NET5_0
using System.Net.Http;
#endif

namespace WitherTorch.Core.Servers
{
    public class PowerNukkit : Server<PowerNukkit>
    {
        private const string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36";
        private const string manifestListURL = "https://repo1.maven.org/maven2/org/powernukkit/powernukkit/maven-metadata.xml";
        private const string downloadURL = "https://repo1.maven.org/maven2/org/powernukkit/powernukkit/{0}/powernukkit-{0}-shaded.jar";
        private static Dictionary<string, string> versionDict = new Dictionary<string, string>();
        private static string[] versions;
        private bool _isStarted;
        private string versionString;
        private SystemProcess process;
        private JavaRuntimeEnvironment environment;
        private IPropertyFile[] propertyFiles = new IPropertyFile[2];

        public PowerNukkit()
        {
            if (IsInit)
            {
                SoftwareRegistrationDelegate += Initialize;
                SoftwareID = "powerNukkit";
            }
        }

        private static void Initialize()
        {
            if (versions == null)
            {
                LoadVersionList();
            }
        }

        private static void LoadVersionList()
        {
            string manifestString = CachedDownloadClient.Instance.DownloadString(manifestListURL);
            if (manifestString != null)
            {
                XmlDocument manifestXML = new XmlDocument();
                manifestXML.LoadXml(manifestString);
                List<string> versionList = new List<string>();
                foreach (XmlNode token in manifestXML.SelectNodes("/metadata/versioning/versions/version"))
                {
                    string rawVersion = token.InnerText;
                    string[] versions = rawVersion.Split(new char[] { '-' }, 3);
                    if (versions.Length == 3) continue;
                    string key = versions[0];
                    if (versionDict.ContainsKey(key))
                    {
                        versionDict[key] = rawVersion;
                    }
                    else
                    {
                        versionDict.Add(key, rawVersion);
                        versionList.Insert(0, key);
                    }
                }
                versions = versionList.ToArray();
            }
            else
            {
                versions = Array.Empty<string>();
            }
        }

        public override bool ChangeVersion(int versionIndex)
        {
            versionString = versions[versionIndex];
            InstallSoftware();
            return true;
        }

        private void InstallSoftware()
        {
            InstallTask installingTask = new InstallTask(this);
            OnInstallSoftware(installingTask);
            if (versionDict.TryGetValue(versionString, out string fullVersionString))
            {
                string downloadURL = string.Format(PowerNukkit.downloadURL, fullVersionString);
                DownloadStatus status = new DownloadStatus(downloadURL, 0);
                installingTask.ChangeStatus(status);
#if NET472
                WebClient client = new WebClient();
                client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    installingTask.ChangePercentage(e.ProgressPercentage);
                };
                client.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
                {
                    client.Dispose();
                    if (e.Error != null || e.Cancelled)
                    {
                        installingTask.OnInstallFailed();
                    }
                    else
                    {
                        installingTask.OnInstallFinished();
                    }
                };
                client.DownloadFileAsync(new Uri(downloadURL), Path.Combine(ServerDirectory, @"powernukkit-" + versionString + ".jar"));
#elif NET5_0
                HttpClientHandler messageHandler = new HttpClientHandler();
                HttpClient client = new HttpClient(messageHandler);
                System.Net.Http.Handlers.ProgressMessageHandler progressHandler = new System.Net.Http.Handlers.ProgressMessageHandler(messageHandler);
                progressHandler.HttpReceiveProgress += delegate (object sender, System.Net.Http.Handlers.HttpProgressEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    installingTask.ChangePercentage(e.ProgressPercentage);
                };
                Task.Run(async () =>
                {
                    try
                    {
                        await InstallUtils.HttpDownload(client, downloadURL, Path.Combine(ServerDirectory, @"powernukkit-" + versionString + ".jar"));
                        installingTask.OnInstallFinished();
                    }
                    catch (Exception)
                    {
                        installingTask.OnInstallFailed();
                    }
                    client.Dispose();
                });
#endif
            }
            else
            {
                installingTask.OnInstallFailed();
            }
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
            return environment;
        }

        public override IPropertyFile[] GetServerPropertyFiles()
        {
            return propertyFiles;
        }

        public override string[] GetSoftwareVersions()
        {
            return versions;
        }

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
                        , Path.Combine(ServerDirectory, @"powernukkit-" + versionString + ".jar")
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

        public override void SetRuntimeEnvironment(RuntimeEnvironment environment)
        {
            if (environment is JavaRuntimeEnvironment runtimeEnvironment)
            {
                this.environment = runtimeEnvironment;
            }
            else if (environment is null)
            {
                this.environment = null;
            }
        }

        public override void UpdateServer()
        {
            ChangeVersion(Array.IndexOf(versions, versionString));
        }

        protected override bool CreateServer()
        {
            try
            {
                process = new SystemProcess();
                process.ProcessStarted += delegate (object sender, EventArgs e) { _isStarted = true; };
                process.ProcessEnded += delegate (object sender, EventArgs e) { _isStarted = false; };
                propertyFiles[0] = new JavaPropertyFile(Path.Combine(ServerDirectory, "./server.properties"));
                propertyFiles[1] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./nukkit.yml"));
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
                propertyFiles[1] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./nukkit.yml"));
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
    }
}
