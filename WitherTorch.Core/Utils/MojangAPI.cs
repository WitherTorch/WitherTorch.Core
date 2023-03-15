using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 提供與 Mojang 相關的公用API，此類別是靜態類別
    /// </summary>
    public static class MojangAPI
    {
        private const string manifestListURL = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
        public static Dictionary<string, VersionInfo> VersionDictionary { get; private set; }

        private static string[] versions;
        public static string[] Versions
        {
            get
            {
                if (versions is null)
                {
                    LoadVersionList();
                }
                return versions;
            }
        }

        public class VersionInfo : IComparable<string>, IComparable<VersionInfo>
        {
            public string ManifestURL { get; }
            public DateTime ReleaseDate { get; }
            public string VersionType { get; }

            public VersionInfo(string versionType, in DateTime releaseDate, string manifestURL)
            {
                VersionType = versionType;
                ReleaseDate = releaseDate;
                ManifestURL = manifestURL;
            }

            int IComparable<string>.CompareTo(string other)
            {
                if (other is null) return 0;
                else if (VersionDictionary.ContainsKey(other))
                {
                    return ReleaseDate.CompareTo(VersionDictionary[other].ReleaseDate);
                }
                return 0;
            }

            int IComparable<VersionInfo>.CompareTo(VersionInfo other)
            {
                if (other is null) return 1;
                else return ReleaseDate.CompareTo(other.ReleaseDate);
            }

            public static bool operator <(VersionInfo a, VersionInfo b)
            {
                return (a as IComparable<VersionInfo>).CompareTo(b) < 0;
            }

            public static bool operator <=(VersionInfo a, VersionInfo b)
            {
                return (a as IComparable<VersionInfo>).CompareTo(b) <= 0;
            }

            public static bool operator >(VersionInfo a, VersionInfo b)
            {
                return (a as IComparable<VersionInfo>).CompareTo(b) > 0;
            }

            public static bool operator >=(VersionInfo a, VersionInfo b)
            {
                return (a as IComparable<VersionInfo>).CompareTo(b) >= 0;
            }
        }

        public static event EventHandler Initialized;

        volatile static bool isInInitialize = false;
        public static void Initialize()
        {
            if (isInInitialize) return;
            isInInitialize = true;
            if (VersionDictionary is null) LoadVersionList();
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
                    JObject manifestJSON;
                    using (JsonTextReader reader = new JsonTextReader(new StringReader(manifestString)))
                    {
                        try
                        {
                            manifestJSON = GlobalSerializers.JsonSerializer.Deserialize(reader) as JObject;
                        }
                        catch (Exception)
                        {
                            manifestJSON = null;
                        }
                        reader.Close();
                    }
                    if (manifestJSON != null)
                    {
                        foreach (JToken token in manifestJSON.GetValue("versions"))
                        {
                            if (token is JObject tokenObj)
                            {
                                string id = null, url = null, type = null, releaseTime = null;
                                foreach (var prop in tokenObj)
                                {
                                    switch (prop.Key)
                                    {
                                        case "id":
                                            id = prop.Value.ToString();
                                            break;
                                        case "url":
                                            url = prop.Value.ToString();
                                            break;
                                        case "type":
                                            type = prop.Value.ToString();
                                            break;
                                        case "releaseTime":
                                            releaseTime = prop.Value.ToString();
                                            break;
                                        default:
                                            continue;
                                    }
                                }
                                if (id is object && url is object && type is object && releaseTime is object &&
                                    DateTime.TryParse(releaseTime, out DateTime trueReleaseTime) && IsValidTime(trueReleaseTime))
                                {
                                    versionPairs.Add(id, new VersionInfo(type, trueReleaseTime, url));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            VersionDictionary = versionPairs;
            versions = versionPairs.Count > 0 ? versionPairs.Keys.ToArray() : Array.Empty<string>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidTime(in DateTime time)
        {
            int year = time.Year;
            int month = time.Month;
            int day = time.Day;
            if (year > 2012 || (year == 2012 && (month > 3 || (month == 3 && day >= 29)))) //1.2.5 開始有 server 版本 (1.2.5 發布日期: 2012/3/29)
            {
                return month != 4 || day != 1; // 過濾愚人節版本
            }
            return false;
        }

        public sealed class VersionComparer : IComparer<string>
        {
            private VersionComparer() { }

            private static VersionComparer _instance = null;

            public static VersionComparer Instance
            {
                get
                {
                    if (_instance is null && VersionDictionary != null) _instance = new VersionComparer();
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
