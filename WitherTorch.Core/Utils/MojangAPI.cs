using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
#if NET472
using System.Net;
using System.Text;
#elif NET5_0
using System.Net.Http;
#endif
using System.IO;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 提供與 Mojang 相關的公用API，此類別是靜態類別
    /// </summary>
    public static class MojangAPI
    {
        private const string manifestListURL = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
        public static Dictionary<string, VersionInfo> VersionDictionary { get; private set; }
        public static string[] Versions { get; private set; }
        public struct VersionInfo : IComparable<string>, IComparable<VersionInfo>
        {
            public string ManifestURL;
            public DateTime ReleaseDate;
            public string VersionType;

            public bool IsEmpty() => ReleaseDate == default;

            int IComparable<string>.CompareTo(string other)
            {
                if (other == null) return 0;
                else if (VersionDictionary.ContainsKey(other))
                {
                    if (IsEmpty()) return -1;
                    else ReleaseDate.CompareTo(VersionDictionary[other].ReleaseDate);
                }
                return 0;
            }

            int IComparable<VersionInfo>.CompareTo(VersionInfo other)
            {
                if (IsEmpty()) return -1;
                else if (other.IsEmpty()) return 1;
                else return ReleaseDate.CompareTo(other.ReleaseDate);
            }

            public static bool operator <(VersionInfo a, VersionInfo b)
            {
                return (a as IComparable<VersionInfo>).CompareTo(b) < 0;
            }

            public static bool operator >(VersionInfo a, VersionInfo b)
            {
                return (a as IComparable<VersionInfo>).CompareTo(b) > 0;
            }
        }

        public static event EventHandler Initialized;

        volatile static bool isInInitialize = false;
        public static void Initialize()
        {
            if (isInInitialize) return;
            isInInitialize = true;
            if (VersionDictionary == null) LoadVersionList();
            Initialized?.Invoke(null, EventArgs.Empty);
            isInInitialize = false;
        }
        public static void LoadVersionList()
        {
            Dictionary<string, VersionInfo> versionPairs = new Dictionary<string, VersionInfo>();
            try
            {
                string manifestString = CachedDownloadClient.Instance.DownloadString(manifestListURL);
                if (manifestString != null)
                {
                    JObject manifestJSON = JsonConvert.DeserializeObject<JObject>(manifestString);
                    if (manifestJSON != null)
                    {
                        foreach (var token in manifestJSON.GetValue("versions").ToObject<JArray>())
                        {
                            try
                            {
                                VersionInfo info = new VersionInfo()
                                {
                                    ManifestURL = token["url"].ToString(),
                                    VersionType = token["type"].ToString(),
                                    ReleaseDate = DateTime.Parse(token["releaseTime"].ToString())
                                };
                                if (info.ReleaseDate.Month == 4 && info.ReleaseDate.Day == 1) continue; // 過濾愚人節版本
                                versionPairs.Add(token["id"].ToString(), info);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            VersionDictionary = versionPairs;
            Versions = versionPairs.Any() ? versionPairs.Keys.ToArray() : Array.Empty<string>();
        }

        public sealed class VersionComparer : IComparer<string>
        {
            private VersionComparer() { }

            private static VersionComparer _instance = null;

            public static VersionComparer Instance
            {
                get
                {
                    if (_instance == null && VersionDictionary != null) _instance = new VersionComparer();
                    return _instance;
                }
            }

            public int Compare(string x, string y)
            {
                bool success = VersionDictionary.TryGetValue(x, out VersionInfo infoA);
                if (!success) return 0;
                success = VersionDictionary.TryGetValue(y, out VersionInfo infoB);
                if (success)
                {
                    return infoA.ReleaseDate.CompareTo(infoB.ReleaseDate);
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
