using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using YamlDotNet.Serialization;

namespace WitherTorch.Core
{
    public class YamlPropertyFile : JsonPropertyFile
    {
        public YamlPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false) : base(path, create, ignoreLazyRequest)
        {
        }

        public override void Reload()
        {
            if (System.IO.File.Exists(_path))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(_path))
                {
                    try
                    {
                        var deserializer = new DeserializerBuilder().Build();
                        var yamlObject = deserializer.Deserialize(reader);
                        deserializer = null;
                        var jsonSerializer = new SerializerBuilder().JsonCompatible().Build();
                        var jsonString = jsonSerializer.Serialize(yamlObject);
                        jsonSerializer = null;
                        currentObject = JsonConvert.DeserializeObject<JObject>(jsonString);
                    }
                    catch (Exception)
                    {

                    }
                    GC.Collect();
                    reader.Close();
                }
            }
            else if (create && currentObject == null)
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
                var expConverter = new Newtonsoft.Json.Converters.ExpandoObjectConverter();
                var deserializedObject = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(JsonConvert.SerializeObject(currentObject), expConverter);
                var serializer = new SerializerBuilder().Build();
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(new System.IO.FileStream(_path, System.IO.FileMode.Create, System.IO.FileAccess.Write)))
                {
                    try
                    {
                        string yaml = serializer.Serialize(deserializedObject);
                        writer.Write(yaml);
                        writer.Flush();
                        writer.Close();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
    }
}
