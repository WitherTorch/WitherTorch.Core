using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WitherTorch.Core.Property
{
    /// <summary>
    /// 表示以 Java 設定檔格式 (.properties) 儲存的設定檔
    /// </summary>
    public sealed class JavaPropertyFile : AbstractPropertyFile
    {
        private Dictionary<string, string>? _propertyDict;
        private Dictionary<int, string>? _descriptionDict;

        private bool _create;

        public JavaPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false) : base(path)
        {
            _create = create;
            if (!WTCore.UseLazyLoadingOnPropertyFiles || ignoreLazyRequest)
                Initialize();
        }

        protected override void ClearCache()
        {
            base.ClearCache();
            _propertyDict = null;
            _descriptionDict = null;
        }

        protected override void Initialize(bool isDirty) => Reload();

        public string? this[string? key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                    return null;
                Initialize();
                Dictionary<string, string>? propertyDict = _propertyDict;
                if (propertyDict is null)
                    return null;
#pragma warning disable CS8604
                return propertyDict.TryGetValue(key, out string? value) ? value : null;
#pragma warning restore CS8604
            }
            set
            {
                if (string.IsNullOrEmpty(key))
                    return;
                Initialize();
                Dictionary<string, string>? propertyDict = _propertyDict;
                if (propertyDict is null)
                    return;
#pragma warning disable CS8604
                if (value is null)
                    propertyDict.Remove(key);
                else
                    propertyDict[key] = value;
#pragma warning restore CS8604
            }
        }

        protected override void Reload(bool isDirty)
        {
            string path = FilePath;
            if (string.IsNullOrEmpty(path))
                return;
            if (!File.Exists(path))
            {
                if (_create)
                {
                    _propertyDict = new Dictionary<string, string>();
                    _descriptionDict = new Dictionary<int, string>();
                }
                return;
            }

            Dictionary<string, string>? propertyDict = _propertyDict;
            Dictionary<int, string>? descriptionDict = _descriptionDict;

            if (propertyDict is null)
            {
                propertyDict = new Dictionary<string, string>();
                _propertyDict = propertyDict;
            }
            else
                propertyDict.Clear();
            if (descriptionDict is null)
            {
                descriptionDict = new Dictionary<int, string>();
                _descriptionDict = descriptionDict;
            }
            else
                descriptionDict.Clear();

            using StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.UTF8);
            int lineIndex = 0;
            for (string? line = reader.ReadLine(); line is not null; line = reader.ReadLine(), lineIndex++)
            {
                if (line.Length <= 0)
                    continue;
                char header = line[0];
                if (header == '!' || header == '#')
                {
                    descriptionDict.Add(lineIndex, line);
                    continue;
                }
                int indexOf = line.IndexOf('=');
                if (indexOf < 0)
                    continue;
                propertyDict[line.Substring(0, indexOf)] = line.Substring(indexOf + 1);
            }
        }

        protected override void Save(bool isDirty, bool force)
        {
            string path = FilePath;
            if (string.IsNullOrEmpty(path) || !force && !isDirty)
                return;
            if (force)
                Initialize();
            using StreamWriter writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8) { AutoFlush = true };
            SetFileWatching(false);
            OutputToWriter(writer, true);
            writer.Close();
            SetFileWatching(true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            Initialize();
            using StringWriter writer = new StringWriter();
            OutputToWriter(writer, false);
            string result = writer.ToString();
            writer.Close();
            return result;
        }

        private void OutputToWriter(TextWriter writer, bool useDescriptor)
        {
            Dictionary<string, string>? propertyDict = _propertyDict;
            Dictionary<int, string>? descriptionDict = _descriptionDict;
            if (propertyDict is null)
                propertyDict = new Dictionary<string, string>();
            else
                propertyDict = new Dictionary<string, string>(propertyDict);
            if (descriptionDict is null)
                descriptionDict = new Dictionary<int, string>();
            else
                descriptionDict = new Dictionary<int, string>(descriptionDict);
            if (useDescriptor)
            {
                IPropertyFileDescriptor? descriptor = Descriptor;
                if (descriptor is not null)
                {
                    PropertyFileNode[] nodes = descriptor.GetNodes();
                    for (int i = 0, length = nodes.Length; i < length; i++)
                    {
                        PropertyFileNode node = nodes[i];
                        if (node.IsOptional)
                            continue;
                        string key = node.Path[0];
                        if (!propertyDict.ContainsKey(key))
                            propertyDict.Add(key, node.DefaultValue);
                    }
                }
            }
            int lineIndex = 0;
            foreach (KeyValuePair<string, string> pair in propertyDict)
            {
                while (descriptionDict.TryGetValue(lineIndex, out string? description) == true)
                {
                    descriptionDict.Remove(lineIndex);
                    writer.WriteLine(description);
                    lineIndex++;
                }
                writer.Write(pair.Key);
                writer.Write('=');
                writer.WriteLine(pair.Value);
                lineIndex++;
            }
            if (descriptionDict.Count > 0)
            {
                int[] keys = descriptionDict.Keys.ToArray();
                Array.Sort(keys);
                foreach (int key in keys)
                {
                    writer.WriteLine(descriptionDict[key]);
                }
            }
            writer.Flush();
        }
    }
}
