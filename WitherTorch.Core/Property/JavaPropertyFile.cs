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
    public sealed class JavaPropertyFile : PropertyFileBase<string>
    {
        private readonly Dictionary<string, string> _propertyDict = new Dictionary<string, string>();
        private readonly Dictionary<int, string> _descriptionDict = new Dictionary<int, string>();

        public JavaPropertyFile(string path) : base(path) { }

        public JavaPropertyFile(string path, bool useFileWatcher) : base(path, useFileWatcher) { }

        protected override void LoadCore(Stream? stream)
        {
            Dictionary<string, string> propertyDict = _propertyDict;
            Dictionary<int, string> descriptionDict = _descriptionDict;

            if (stream is null)
            {
                propertyDict.Clear();
                descriptionDict.Clear();
                return;
            }

            using StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, bufferSize: 4096, leaveOpen: true);
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
                ReadOnlySpan<char> keySpan = line.AsSpan().Slice(0, indexOf).Trim();
                if (keySpan.IsEmpty)
                    continue;
                propertyDict[keySpan.ToString()] = line.Substring(indexOf + 1);
            }
        }

        protected override void UnloadCore()
        {
            _propertyDict.Clear();
            _descriptionDict.Clear();
        }

        protected override void SaveCore(Stream stream)
        {
            using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
            OutputToWriter(writer, useDescriptor: true);
            writer.Flush();
        }

        protected override string? GetValueCore(string key)
        {
            return _propertyDict.TryGetValue(key, out string? result) ? result : null;
        }

        protected override bool SetValueCore(string key, string value)
        {
            _propertyDict[key] = value;
            return true;
        }

        protected override bool RemoveValueCore(string key)
        {
            return _propertyDict.Remove(key);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            Load(force: false);
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
