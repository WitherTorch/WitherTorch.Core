using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if NET472
using System.Net;
using System.Runtime.CompilerServices;
#elif NET5_0
using System.Net.Http;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// Forge 伺服器
    /// </summary>
    public class Forge : AbstractJavaEditionServer<Forge>
    {
        private const string manifestListURL = "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml";
        private const string downloadURLPrefix = "https://maven.minecraftforge.net/net/minecraftforge/forge/";
        private static StringBuilder URLBuilder = null;
        internal static string[] versions;
        internal static Dictionary<string, List<Tuple<string, string>>> versionDict;
        protected bool _isStarted;

        protected SystemProcess process;
        private string versionString;
        private string forgeVersion;
        private JavaRuntimeEnvironment environment;
        IPropertyFile[] propertyFiles = new IPropertyFile[1];
        public JavaPropertyFile ServerPropertiesFile => propertyFiles[0] as JavaPropertyFile;
        private static MojangAPI.VersionInfo mc1_3_2, mc1_5_2;

        public Forge()
        {
            if (IsInit)
            {
                SoftwareRegistrationDelegate += Initialize;
                SoftwareID = "forge";
            }
        }

        private static void Initialize()
        {
            if (mc1_3_2.IsEmpty()) MojangAPI.VersionDictionary?.TryGetValue("1.3.2", out mc1_3_2);
            if (mc1_5_2.IsEmpty()) MojangAPI.VersionDictionary?.TryGetValue("1.5.2", out mc1_5_2);
            LoadVersionList();
        }

        internal static void LoadVersionList()
        {
            Dictionary<string, List<Tuple<string, string>>> preparingVersionDict = new Dictionary<string, List<Tuple<string, string>>>();
            try
            {
                string manifestString = CachedDownloadClient.Instance.DownloadString(manifestListURL);
                if (manifestString != null)
                {
                    XmlDocument manifestXML = new XmlDocument();
                    manifestXML.LoadXml(manifestString);
                    List<Tuple<string, string>> historyVersionList = null;
                    foreach (XmlNode token in manifestXML.SelectNodes("/metadata/versioning/versions/version"))
                    {
                        string[] versionSplits = token.InnerText.Split(new char[] { '-' });
                        string version;
                        unsafe
                        {
                            fixed (char* rawVersionString = versionSplits[0])
                            {
                                char* rawVersionStringEnd = rawVersionString + versionSplits[0].Length;
                                char* pointerChar = rawVersionString;
                                while (pointerChar < rawVersionStringEnd)
                                {
                                    if (*pointerChar == '_')
                                    {
                                        *pointerChar = '-';
                                        break;
                                    }
                                    pointerChar++;
                                }
                                version = new string(rawVersionString);
                            }
                        }
                        if (!preparingVersionDict.ContainsKey(version))
                        {
                            historyVersionList = new List<Tuple<string, string>>();
                            preparingVersionDict.Add(version, historyVersionList);
                        }
                        historyVersionList?.Add(new Tuple<string, string>(versionSplits[1], token.InnerText));
                    }
                }
            }
            catch (Exception)
            {

            }
            versionDict = new Dictionary<string, List<Tuple<string, string>>>();
            IEnumerable<string> keys = preparingVersionDict.Keys;
            List<string> versionKeys = new List<string>(keys.Count());
            versionKeys.AddRange(keys);
            var comparer = MojangAPI.VersionComparer.Instance;
            if (comparer == null)
            {
                using (ManualResetEvent trigger = new ManualResetEvent(false))
                {
                    void trig(object sender, EventArgs e)
                    {
                        trigger.Set();
                    }
                    MojangAPI.Initialized += trig;
                    if (MojangAPI.VersionDictionary == null)
                    {
                        trigger.WaitOne();
                    }
                    MojangAPI.Initialized -= trig;
                    comparer = MojangAPI.VersionComparer.Instance;
                }
            }
            versions = versionKeys.ToArray();
            Array.Sort(versions, comparer);
            Array.Reverse(versions);
            for (int i = 0;i< versions.Length; i++)
            {
                string key = versions[i];
                versionDict.Add(key, preparingVersionDict[key]);
            }
        }

        public bool ChangeVersion(int versionIndex, string forgeVersion)
        {
            try
            {
                if (versions == null) LoadVersionList();
                versionString = versions[versionIndex];
                BuildVersionInfo();
                Tuple<string, string> selectedVersion;
                if (forgeVersion == null)
                {
                    selectedVersion = versionDict[versionString].First();
                }
                else
                {
                    selectedVersion = versionDict[versionString].Find(x => x.Item1 == forgeVersion);
                    if (selectedVersion == null)
                        return false;
                }
                this.forgeVersion = selectedVersion.Item1;
                _cache = null;
                InstallSoftware(selectedVersion);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public override bool ChangeVersion(int versionIndex)
        {
            return ChangeVersion(versionIndex, null);
        }

        public void InstallSoftware(Tuple<string, string> selectedVersion)
        {
#if NET472
            WebClient client = new WebClient();
#elif NET5_0
            HttpClientHandler messageHandler = new HttpClientHandler();
            HttpClient client = new HttpClient(messageHandler);
#endif
            bool needInstall = false;
            InstallTask installingTask = new InstallTask(this);
            OnInstallSoftware(installingTask);
            string downloadURL = null;
            if (URLBuilder == null)
            {
                URLBuilder = new StringBuilder(downloadURLPrefix, 114);
            }
            else
            {
                URLBuilder.Append(downloadURLPrefix);
            }
            if (mc1_3_2.IsEmpty()) MojangAPI.VersionDictionary.TryGetValue("1.3.2", out mc1_3_2);
            if (mojangVersionInfo < mc1_3_2) // 1.1~1.2 > Download Server Zip (i don't know why forge use zip...)
            {
                URLBuilder.AppendFormat("{0}/forge-{0}-server.zip", selectedVersion.Item2);
                downloadURL = URLBuilder.ToString();
            }
            else
            {
                if (mc1_5_2.IsEmpty()) MojangAPI.VersionDictionary.TryGetValue("1.5.2", out mc1_5_2);
                if (mojangVersionInfo < mc1_5_2) // 1.3.2~1.5.1 > Download Universal Zip (i don't know why forge use zip...)
                {
                    URLBuilder.AppendFormat("{0}/forge-{0}-universal.zip", selectedVersion.Item2);
                    downloadURL = URLBuilder.ToString();
                }
                else  // 1.5.2 or above > Download Installer (*.jar)
                {
                    URLBuilder.AppendFormat("{0}/forge-{0}-installer.jar", selectedVersion.Item2);
                    downloadURL = URLBuilder.ToString();
                    needInstall = true;
                }
            }
            URLBuilder.Clear();
            if (downloadURL != null)
            {
                DownloadStatus status = new DownloadStatus(downloadURL, 0);
#if NET472
                string installerLocation = null;
                client.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    if (needInstall)
                    {
                        installingTask.ChangePercentage(e.ProgressPercentage / 2);
                    }
                    else
                    {
                        installingTask.ChangePercentage(e.ProgressPercentage);
                    }
                };
                client.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
                {
                    client.Dispose();
                    if (e.Error != null || e.Cancelled == true)
                    {
                        installingTask.OnInstallFailed();
                    }
                    else
                    {
                        if (needInstall)
                        {
                            try
                            {
                                RunInstaller(installingTask, installerLocation);
                            }
                            catch (Exception)
                            {
                                installingTask.OnInstallFailed();
                            }
                        }
                        else
                        {
                            installingTask.OnInstallFinished();
                        }
                    }
                };
                installingTask.ChangeStatus(status);
                if (needInstall)
                {
                    installerLocation = Path.Combine(ServerDirectory, @"forge-" + selectedVersion.Item2 + "-installer.jar");
                    client.DownloadFileAsync(new Uri(downloadURL), installerLocation);
                }
                else
                {
                    client.DownloadFileAsync(new Uri(downloadURL), Path.Combine(ServerDirectory, @"forge-" + selectedVersion.Item2 + ".jar"));
                }
#elif NET5_0
                System.Net.Http.Handlers.ProgressMessageHandler progressHandler = new System.Net.Http.Handlers.ProgressMessageHandler(messageHandler);
                progressHandler.HttpReceiveProgress += delegate (object sender, System.Net.Http.Handlers.HttpProgressEventArgs e)
                {
                    status.Percentage = e.ProgressPercentage;
                    installingTask.OnStatusChanged();
                    if (needInstall)
                    {
                        installingTask.ChangePercentage(e.ProgressPercentage / 2);
                    }
                    else
                    {
                        installingTask.ChangePercentage(e.ProgressPercentage);
                    }
                };
                Task.Run(async () =>
                {
                    try
                    {
                        string installerLocation;
                        if (needInstall)
                        {
                            installerLocation = Path.Combine(ServerDirectory, @"forge-" + selectedVersion.Item2 + "-installer.jar");
                        }
                        else
                        {
                            installerLocation = Path.Combine(ServerDirectory, @"forge-" + selectedVersion.Item2 + ".jar");
                        }
                        await InstallUtils.HttpDownload(client, downloadURL, installerLocation);
                        if (needInstall)
                            RunInstaller(installingTask, installerLocation);
                        else
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
        }

        private void RunInstaller(InstallTask task, in string jarPath)
        {
            ProcessStatus installStatus = new ProcessStatus(50);
            task.ChangeStatus(installStatus);
            task.ChangePercentage(50);
            JavaRuntimeEnvironment environment = RuntimeEnvironment.JavaDefault;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = environment.JavaPath,
                Arguments = string.Format("-Xms512M -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 -jar \"{0}\" nogui --installServer", jarPath),
                WorkingDirectory = ServerDirectory,
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
                task.OnInstallFinished();
                task.ChangePercentage(100);
                innerProcess.Dispose();
            };
        }


        public override AbstractProcess GetProcess()
        {
            return process;
        }

        string _cache;
        public override string GetReadableVersion()
        {
            if (_cache == null)
            {
                _cache = versionString + "-" + forgeVersion;
            }
            return _cache;
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
            if (versions == null)
            {
                LoadVersionList();
            }
            return versions;
        }

        private string GetFullVersionString()
        {
            return versionDict[versionString].Find(tuple => tuple.Item1 == forgeVersion).Item2;
        }

        public override void RunServer(RuntimeEnvironment environment)
        {
            if (!_isStarted)
            {
                if (environment is null)
                    environment = RuntimeEnvironment.JavaDefault;
                if (environment is JavaRuntimeEnvironment javaRuntimeEnvironment)
                {
                    ProcessStartInfo startInfo = null;
                    string fullVersionString = GetFullVersionString();
                    string path = Path.Combine(ServerDirectory, "forge-" + fullVersionString + ".jar");
                    if (File.Exists(path))
                    {
                        string javaPath = javaRuntimeEnvironment.JavaPath;
                        if (javaPath is null || !File.Exists(javaPath))
                        {
                            javaPath = RuntimeEnvironment.JavaDefault.JavaPath;
                        }
                        startInfo = new ProcessStartInfo
                        {
                            FileName = javaPath,
                            Arguments = string.Format("-Dfile.encoding=UTF8 -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 {0} -jar \"{1}\" {2}"
                            , javaRuntimeEnvironment.JavaPreArguments ?? RuntimeEnvironment.JavaDefault.JavaPreArguments
                            , path
                            , javaRuntimeEnvironment.JavaPostArguments ?? RuntimeEnvironment.JavaDefault.JavaPostArguments),
                            WorkingDirectory = ServerDirectory,
                            CreateNoWindow = true,
                            ErrorDialog = true,
                            UseShellExecute = false,
                        };
                    }
                    else
                    {
                        string argPath = "@libraries/net/minecraftforge/forge/" + fullVersionString;
#if NET472
                        switch (Environment.OSVersion.Platform)
                        {
                            case PlatformID.Win32NT:
                                argPath += "/win_args.txt";
                                break;
                            case PlatformID.Unix:
                                argPath += "/unix_args.txt";
                                break;
                        }
#elif NET5_0
                        if (OperatingSystem.IsWindows())
                        {
                            argPath += "/win_args.txt";
                        }
                        else if (OperatingSystem.IsLinux())
                        {
                            argPath += "/unix_args.txt";
                        }
                        else
                        {
                            switch (Environment.OSVersion.Platform)
                            {
                                case PlatformID.Unix:
                                    argPath += "/unix_args.txt";
                                    break;
                            }
                        }
#endif
                        if (File.Exists(Path.Combine(ServerDirectory, "./" + argPath.Substring(1))))
                        {
                            string javaPath = javaRuntimeEnvironment.JavaPath;
                            if (javaPath is null || !File.Exists(javaPath))
                            {
                                javaPath = RuntimeEnvironment.JavaDefault.JavaPath;
                            }
                            startInfo = new ProcessStartInfo
                            {
                                FileName = javaPath,
                                Arguments = string.Format("-Dfile.encoding=UTF8 -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 {0} {1} {2}"
                                , javaRuntimeEnvironment.JavaPreArguments ?? RuntimeEnvironment.JavaDefault.JavaPreArguments
                                , argPath
                                , javaRuntimeEnvironment.JavaPostArguments ?? RuntimeEnvironment.JavaDefault.JavaPostArguments),
                                WorkingDirectory = ServerDirectory,
                                CreateNoWindow = true,
                                ErrorDialog = true,
                                UseShellExecute = false,
                            };
                        }
                    }
                    if (startInfo != null)
                    {
                        process.StartProcess(startInfo);
                    }
                }
            }
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
                JToken forgeVerNode = serverInfoJson["forge-version"];
                if (forgeVerNode?.Type == JTokenType.String)
                {
                    forgeVersion = forgeVerNode.ToString();
                }
                else
                {
                    return false;
                }
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
        public override void SetRuntimeEnvironment(RuntimeEnvironment environment)
        {
            if (environment is JavaRuntimeEnvironment javaRuntimeEnvironment)
                this.environment = javaRuntimeEnvironment;
            else if (environment is null)
                this.environment = null;
        }

        protected override bool OnServerSaving()
        {
            JsonPropertyFile serverInfoJson = ServerInfoJson;
            serverInfoJson["version"] = versionString;
            serverInfoJson["forge-version"] = forgeVersion;
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
            if (versions == null) LoadVersionList();
            return ChangeVersion(Array.IndexOf(versions, versionString));
        }

    }
}
