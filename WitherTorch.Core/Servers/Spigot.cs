using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Servers
{
    /// <summary>
    /// Spigot 伺服器
    /// </summary>
    public class Spigot : AbstractJavaEditionServer<Spigot>
    {
        protected bool _isStarted;
        IPropertyFile[] propertyFiles = new IPropertyFile[3];
        public JavaPropertyFile ServerPropertiesFile => propertyFiles[0] as JavaPropertyFile;
        public YamlPropertyFile BukkitYMLFile => propertyFiles[1] as YamlPropertyFile;
        public YamlPropertyFile SpigotYMLFile => propertyFiles[2] as YamlPropertyFile;
        private string versionString;
        private int build = -1;
        private JavaRuntimeEnvironment environment;
        protected SystemProcess process;

        public Spigot()
        {
            if (IsInit)
            {
                SoftwareRegistrationDelegate += SpigotAPI.Initialize;
                SoftwareID = "spigot";
            }
        }

        InstallTask installingTask;
        private void InstallSoftware()
        {
            installingTask = new InstallTask(this);
            OnInstallSoftware(installingTask);
            SpigotBuildTools.Instance.Install(installingTask, SpigotBuildTools.BuildTarget.Spigot, GetReadableVersion());
        }

        public override bool ChangeVersion(int versionIndex)
        {
            try
            {
                versionString = SpigotAPI.Versions[versionIndex];
                BuildVersionInfo();
                build = SpigotAPI.GetBuildNumber(versionString);
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
            return SpigotAPI.Versions;
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
                    build = (int)buildNode;
                }
                else
                {
                    build = 0;
                }
                process = new SystemProcess();
                process.ProcessStarted += delegate (object sender, EventArgs e) { _isStarted = true; };
                process.ProcessEnded += delegate (object sender, EventArgs e) { _isStarted = false; };
                propertyFiles[0] = new JavaPropertyFile(Path.Combine(ServerDirectory, "./server.properties"));
                propertyFiles[1] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./bukkit.yml"));
                propertyFiles[2] = new YamlPropertyFile(Path.Combine(ServerDirectory, "./spigot.yml"));
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
                        , Path.Combine(ServerDirectory, @"spigot-" + GetReadableVersion() + ".jar")
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
            ChangeVersion(Array.IndexOf(SpigotAPI.Versions, versionString));
        }
    }
}
