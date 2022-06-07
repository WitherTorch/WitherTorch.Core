using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// Fabric 伺服器
    /// </summary>
    public class Fabric : AbstractJavaEditionServer<Fabric>
    {
        private const string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36";
        private const string manifestListURL = "https://meta.fabricmc.net/v2/versions/game";
        private const string manifestListURLForLoader = "https://meta.fabricmc.net/v2/versions/loader";
        internal static List<string> versionList;
        protected bool _isStarted;

        protected SystemProcess process;
        private string versionString;
        private string fabricVersion;
        private JavaRuntimeEnvironment environment;
        IPropertyFile[] propertyFiles = new IPropertyFile[1];
        public JavaPropertyFile ServerPropertiesFile => propertyFiles[0] as JavaPropertyFile;

        public Fabric()
        {
            if (IsInit)
            {
                SoftwareRegistrationDelegate += Initialize;
                SoftwareID = "fabric";
            }
        }

        private static void Initialize()
        {
            if (versionList == null)
            {
                LoadVersionList();
            }
        }

        private static void LoadVersionList()
        {
            versionList = new List<string>();
            try
            {
                string manifestString = CachedDownloadClient.Instance.DownloadString(manifestListURL);
                if (manifestString != null)
                {
                    JArray jsonArray = JsonConvert.DeserializeObject<JArray>(manifestString);
                    foreach (JToken token in jsonArray)
                    {
                        if (token is JObject tokenObject && tokenObject.TryGetValue("version", out JToken versionToken) && versionToken.Type == JTokenType.String)
                        {
                            string versionString = versionToken.ToString();
                            if (MojangAPI.Versions.FirstOrDefault(str => str == versionString) != default)
                            {
                                versionList.Add(versionString);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            var comparer = MojangAPI.VersionComparer.Instance;
            if (comparer == null)
            {
                using (AutoResetEvent trigger = new AutoResetEvent(false))
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
            versionList.Sort(comparer);
            versionList.Reverse();
        }

        public override bool ChangeVersion(int versionIndex)
        {
            try
            {
                versionString = versionList[versionIndex];
                BuildVersionInfo();
                fabricVersion = GetLatestFabricLoaderVersion();
                _cache = null;
                InstallSoftware();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool ChangeVersion(int versionIndex, string fabricVersion)
        {
            try
            {
                versionString = versionList[versionIndex];
                BuildVersionInfo();
                this.fabricVersion = fabricVersion;
                _cache = null;
                InstallSoftware(fabricVersion);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        InstallTask installingTask;
        private void InstallSoftware()
        {
            installingTask = new InstallTask(this);
            OnInstallSoftware(installingTask);
            FabricInstaller.Instance.Install(installingTask, versionString);
        }

        private void InstallSoftware(string fabricVersion)
        {
            installingTask = new InstallTask(this);
            OnInstallSoftware(installingTask);
            FabricInstaller.Instance.Install(installingTask, versionString, fabricVersion);
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
                _cache = versionString + "-" + fabricVersion;
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
            return versionList.ToArray();
        }

        public static string GetLatestFabricLoaderVersion()
        {
            string result = string.Empty;
            try
            {
                JArray jsonArray;
#if NET472
                using (WebClient client = new WebClient() { Encoding = Encoding.UTF8 })
                {
                    client.Headers.Set(HttpRequestHeader.UserAgent, UserAgent);
                    jsonArray = JsonConvert.DeserializeObject<JArray>(client.DownloadString(manifestListURLForLoader));
                }
#elif NET5_0
                using (HttpClient client = new HttpClient())
                {
                    jsonArray = JsonConvert.DeserializeObject<JArray>(client.GetStringAsync(manifestListURLForLoader).Result);
                }
#endif
                foreach (JToken token in jsonArray)
                {
                    if (token is JObject tokenObject && tokenObject.TryGetValue("version", out JToken versionToken) && versionToken.Type == JTokenType.String)
                    {
                        string versionString = versionToken.ToString();
                        if (tokenObject.TryGetValue("stable", out JToken loaderVersionToken) && loaderVersionToken.Type == JTokenType.Boolean && loaderVersionToken.Value<bool>() == true)
                        {
                            result = versionString;
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            return result;
        }

        public override void RunServer(RuntimeEnvironment environment)
        {
            if (!_isStarted)
            {
                if (environment is null)
                    environment = RuntimeEnvironment.JavaDefault;
                if (environment is JavaRuntimeEnvironment javaRuntimeEnvironment)
                {
                    string path = Path.Combine(ServerDirectory, "fabric-server-launch.jar");
                    if (File.Exists(path))
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = javaRuntimeEnvironment.JavaPath ?? RuntimeEnvironment.JavaDefault.JavaPath,
                            Arguments = string.Format("-Dfile.encoding=UTF8 -Dsun.stdout.encoding=UTF8 -Dsun.stderr.encoding=UTF8 {0} -jar \"{1}\" {2}"
                            , javaRuntimeEnvironment.JavaPreArguments ?? RuntimeEnvironment.JavaDefault.JavaPreArguments
                            , path
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
                propertyFiles[0] = new JavaPropertyFile(Path.GetFullPath(Path.Combine(ServerDirectory, "./server.properties")));
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
                JToken forgeVerNode = serverInfoJson["fabric-version"];
                if (forgeVerNode?.Type == JTokenType.String)
                {
                    fabricVersion = forgeVerNode.ToString();
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
            serverInfoJson["fabric-version"] = fabricVersion;
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
            ChangeVersion(versionList.IndexOf(versionString));
        }
    }
}
