using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if NET472
using System.Text;
#endif
using System.Xml;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 提供與 SpigotMC 相關的公用API，此類別是靜態類別
    /// </summary>
    public static class SpigotAPI
    {
        private const string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.55 Safari/537.36";
        private const string manifestListURL = "https://hub.spigotmc.org/nexus/content/groups/public/org/spigotmc/spigot-api/maven-metadata.xml";
        private const string manifestListURL2 = "https://hub.spigotmc.org/nexus/content/groups/public/org/spigotmc/spigot-api/{0}/maven-metadata.xml";

        private static string[] versions;
        public static string[] Versions
        {
            get
            {
                if (versions == null)
                {
                    LoadVersionList();
                }
                return versions;
            }
        }
        public static Dictionary<string, string> VersionDictionary { get; private set; }

        private static volatile bool _isInInitialize = false;
        public static void Initialize()
        {
            if (!_isInInitialize)
            {
                _isInInitialize = true;
                if (Versions == null)
                {
                    LoadVersionList();
                }
                _isInInitialize = false;
            }
        }

        internal static void LoadVersionList()
        {
            Dictionary<string, string> preparingVersionDict = new Dictionary<string, string>();
            try
            {
                string manifestString = CachedDownloadClient.Instance.DownloadString(manifestListURL);
                if (manifestString != null)
                {
                    XmlDocument manifestXML = new XmlDocument();
                    manifestXML.LoadXml(manifestString);
                    foreach (XmlNode token in manifestXML.SelectNodes("/metadata/versioning/versions/version"))
                    {
                        string[] versionSplits = token.InnerText.Split('-');
                        string version = versionSplits[0];
                        for (int i = 1; i < versionSplits.Length; i++)
                        {
                            if (versionSplits[i][0] == 'R' || versionSplits[i] == "SNAPSHOT")
                                break;
                            version += "-" + versionSplits[i];
                        }
                        if (!preparingVersionDict.ContainsKey(version))
                        {
                            preparingVersionDict.Add(version, token.InnerText);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            VersionDictionary = new Dictionary<string, string>();
            foreach (var item in preparingVersionDict.Reverse())
            {
                VersionDictionary.Add(item.Key, item.Value);
            }
            versions = VersionDictionary.Keys.ToArray();
        }

        public static int GetBuildNumber(string version)
        {
            if (VersionDictionary.ContainsKey(version))
            {
                string url = string.Format(manifestListURL2, VersionDictionary[version]);
                try
                {
                    XmlDocument manifestXML = new XmlDocument();
#if NET472
                    using (System.Net.WebClient client = new System.Net.WebClient() { Encoding = Encoding.UTF8 })
                    {
                        client.Headers.Set(System.Net.HttpRequestHeader.UserAgent, UserAgent);
                        manifestXML.LoadXml(client.DownloadString(url));
                    }
#elif NET5_0
                    using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                    {
                        manifestXML.LoadXml(client.GetStringAsync(manifestListURL).Result);
                    }
#endif
                    string buildNumber = manifestXML.SelectSingleNode("/metadata/versioning/snapshot/buildNumber")?.Value;
                    if (buildNumber != null && int.TryParse(buildNumber, out int result))
                    {
                        return result;
                    }
                }
                catch (Exception)
                {

                }
            }
            return -1;
        }
    }

}
