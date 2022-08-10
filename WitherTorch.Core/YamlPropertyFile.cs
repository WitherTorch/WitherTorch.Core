using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace WitherTorch.Core
{
    public class YamlPropertyFile : JsonPropertyFile
    {
        protected static IDeserializer yamlDeserializer;
        protected static ISerializer yamlSerializer;

        public YamlPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false) : base(path, create, ignoreLazyRequest)
        {
        }

        public override void Reload()
        {
            if (System.IO.File.Exists(_path))
            {
                using (StreamReader reader = new StreamReader(_path))
                {
                    try
                    {
                        if (yamlDeserializer == null)
                        {
                            yamlDeserializer = new DeserializerBuilder().Build();
                        }
                        Dictionary<object, object> yamlObject = yamlDeserializer.Deserialize(reader) as Dictionary<object, object>;
                        if (yamlObject != null)
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
                if (yamlSerializer == null)
                {
                    yamlSerializer = new SerializerBuilder().Build();
                }
                using (StreamWriter writer = new StreamWriter(new FileStream(_path, System.IO.FileMode.Create, System.IO.FileAccess.Write)))
                {
                    try
                    {
                        yamlSerializer.Serialize(writer, currentObject.ToObject<System.Dynamic.ExpandoObject>());
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
