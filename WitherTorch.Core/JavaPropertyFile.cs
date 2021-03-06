using System;
using System.Collections.Generic;
using System.Linq;

namespace WitherTorch.Core
{
    public class JavaPropertyFile : IPropertyFile
    {
        private IPropertyFileDescriptor descriptor;
        private string _path;
        private Dictionary<string, string> currentObject;
        private Dictionary<int, string> descriptionDict;
        private bool create;
        private bool isInitialized;

        bool isDirty = false;
        public JavaPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false)
        {
            _path = path;
            this.create = create;
            if (!WTCore.UseLazyLoadingOnPropertyFiles || ignoreLazyRequest)
            {
                Initialize();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void Initialize()
        {
            isInitialized = true;
            Reload();
        }

        public void Dispose()
        {
            _path = "";
            currentObject = null;
        }

        public string this[string key]
        {
            get
            {
                if (key == null) return null;
                else
                {
                    if (!isInitialized)
                        Initialize();
                    if (currentObject.ContainsKey(key))
                        return currentObject[key];
                    else return null;
                }
            }
            set
            {
                if (key == null) return;
                else
                {
                    if (!isInitialized)
                        Initialize();
                    if (currentObject.ContainsKey(key))
                    {
                        if (value == null)
                            currentObject.Remove(key);
                        else
                            currentObject[key] = value;
                    }
                    else currentObject.Add(key, value);
                    isDirty = true;
                }
            }
        }

        public void Reload()
        {
            if (System.IO.File.Exists(_path))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(_path))
                {
                    try
                    {
                        currentObject = new Dictionary<string, string>();
                        descriptionDict = new Dictionary<int, string>();
                        int lineIndex = 0;
                        while (reader.Peek() != -1)
                        {
                            string line = reader.ReadLine();
                            if (string.IsNullOrEmpty(line)) continue;
                            char header = line[0];
                            if (header == '!' || header == '#')
                            {
                                descriptionDict.Add(lineIndex, line);
                            }
                            else
                            {
                                string[] lines = line.Split(new char[] { '=' }, 2);
                                if (!currentObject.ContainsKey(lines[0]))
                                    currentObject.Add(lines[0], lines[1]);
                            }
                            lineIndex++;
                        }
                    }
                    catch (Exception)
                    {

                    }
                    GC.Collect();
                    reader.Close();
                }
            }
            else if (create)
            {
                currentObject = new Dictionary<string, string>();
                descriptionDict = new Dictionary<int, string>();
            }
        }

        public void Save(bool force)
        {
            if (force && !isInitialized)
            {
                Initialize();
            }
            if ((isDirty && isInitialized) || force)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(new System.IO.FileStream(_path, System.IO.File.Exists(_path) ? System.IO.FileMode.Truncate : System.IO.FileMode.CreateNew, System.IO.FileAccess.Write)))
                {
                    try
                    {
                        Queue<PropertyFileNode> defaultNodes = null;
                        if (descriptor != null)
                        {
                            defaultNodes = new Queue<PropertyFileNode>();
                            foreach (PropertyFileNode node in descriptor.GetNodes())
                            {
                                if (!(node.IsOptional || currentObject.ContainsKey(node.Path[0]))) defaultNodes.Enqueue(node);
                            }
                        }
                        int totalLine = currentObject.Count + descriptionDict.Count + (defaultNodes == null ? 0 : defaultNodes.Count);
                        int lineIndex = 0, keyValueIndex = 0;
                        KeyValuePair<string, string>[] keyValueArray = currentObject.ToArray();
                        while (lineIndex < totalLine)
                        {
#if NET472
                            if (descriptionDict.ContainsKey(lineIndex))
                            {
                                writer.WriteLine(descriptionDict[lineIndex]);
                            }
                            else if (keyValueIndex < keyValueArray.Length)
                            {
                                KeyValuePair<string, string> kvPair = keyValueArray[keyValueIndex];
                                writer.WriteLine(kvPair.Key + "=" + kvPair.Value);
                                keyValueIndex++;
                            }
                            else
                            {
                                PropertyFileNode node = defaultNodes.Dequeue();
                                writer.WriteLine(node.Path[0] + "=" + node.DefaultValue);
                            }
                            lineIndex++;
#elif NET5_0
                        if (descriptionDict.TryGetValue(lineIndex, out string line))
                        {
                            writer.WriteLine(line);
                        }
                        else
                        {
                            KeyValuePair<string, string> kvPair = keyValueArray[keyValueIndex];
                            writer.WriteLine(kvPair.Key + "=" + kvPair.Value);
                            keyValueIndex++;
                        }
                        lineIndex++;
#endif
                        }
                        writer.Flush();
                        writer.Close();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
        public IPropertyFileDescriptor GetDescriptor()
        {
            return descriptor;
        }

        public void SetDescriptor(IPropertyFileDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!isInitialized)
                Initialize();
            try
            {
                string result = "";
                int totalLine = currentObject.Count + descriptionDict.Count;
                int lineIndex = 0, keyValueIndex = 0;
                KeyValuePair<string, string>[] keyValueArray = currentObject.ToArray();
                while (lineIndex < totalLine)
                {
#if NET472
                    if (descriptionDict.ContainsKey(lineIndex))
                    {
                        result += descriptionDict[lineIndex] + "\n";
                    }
                    else
                    {
                        KeyValuePair<string, string> kvPair = keyValueArray[keyValueIndex];
                        result += kvPair.Key + "=" + kvPair.Value + "\n";
                        keyValueIndex++;
                    }
                    lineIndex++;
#elif NET5_0
                        if (descriptionDict.TryGetValue(lineIndex, out string line))
                        {
                            result += line + "\n";
                        }
                        else
                        {
                            KeyValuePair<string, string> kvPair = keyValueArray[keyValueIndex];
                            result += kvPair.Key + "=" + kvPair.Value + "\n";
                            keyValueIndex++;
                        }
                        lineIndex++;
#endif
                }
                return result.TrimEnd('\n');
            }
            catch (Exception)
            {

            }
            return "";
        }

        public string GetFilePath()
        {
            return _path;
        }
    }
}
