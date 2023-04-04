using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示以 YAML 格式 (.yaml) 儲存的設定檔
    /// </summary>
    public class YamlPropertyFile : JsonPropertyFile
    {
        public YamlPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false) : base(path, create, ignoreLazyRequest)
        {
        }

        public override void Reload()
        {
            if (File.Exists(_path))
            {
                using (StreamReader reader = new StreamReader(_path))
                {
                    try
                    {
                        if (GlobalSerializers.YamlDeserializer.Deserialize(reader) is Dictionary<object, object> yamlObject)
                        {
                            currentObject = JObject.FromObject(yamlObject);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    try
                    {
                        reader.Close();
                    }
                    catch (IOException)
                    {
                    }
                    catch (NullReferenceException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
            else if (create && currentObject is null)
            {
                currentObject = new JObject();
            }
        }

        public override void Save(bool force)
        {
            if (force && !isInitialized)
            {
                Initialize();
            }
            if ((isDirty && isInitialized) || force)
            {
                using (StreamWriter writer = new StreamWriter(new FileStream(_path, FileMode.Create, FileAccess.Write)))
                {
                    try
                    {
                        GlobalSerializers.YamlSerializer.Serialize(writer, currentObject.ToObject<System.Dynamic.ExpandoObject>());
                        writer.Flush();
                        writer.Close();
                        create = false;
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
    }
}
