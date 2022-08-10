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
        protected static ISerializer yamlToJsonSerializer;
        protected static JsonSerializer jsonExpandoSerializer;
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
                        if (jsonSerializer == null)
                        {
                            jsonSerializer = JsonSerializer.CreateDefault();
                            jsonSerializer.Formatting = Formatting.Indented;
                        }
                        if (yamlDeserializer == null)
                        {
                            yamlDeserializer = new DeserializerBuilder().Build();
                        }
                        if (yamlToJsonSerializer == null)
                        {
                            yamlToJsonSerializer = new SerializerBuilder().JsonCompatible().Build();
                        }
                        object yamlObject = yamlDeserializer.Deserialize(reader);
                        string jsonString = yamlToJsonSerializer.Serialize(yamlObject);
                        using (StringReader stringReader = new StringReader(jsonString))
                        {
                            using (JsonTextReader jsonReader = new JsonTextReader(stringReader))
                            {
                                object content = jsonSerializer.Deserialize(jsonReader);
                                if (content is JObject obj)
                                {
                                    currentObject = obj;
                                }
                                else
                                {
                                    try
                                    {
                                        currentObject = new JObject(content);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                            try
                            {
                                stringReader.Close();
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
                if (jsonSerializer == null)
                {
                    jsonSerializer = JsonSerializer.CreateDefault();
                    jsonSerializer.Formatting = Formatting.Indented;
                }
                if (jsonExpandoSerializer == null)
                {
                    jsonExpandoSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings() { Converters = new List<JsonConverter>() { new ExpandoObjectConverter() } });
                }
                if (yamlSerializer == null)
                {
                    yamlSerializer = new SerializerBuilder().Build();
                }
                System.Dynamic.ExpandoObject deserializedObject;
                using (StringWriter sw = new StringWriter())
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        jsonSerializer.Serialize(writer, currentObject);
                    }
                    sw.Flush();
                    using (StringReader sr = new StringReader(sw.ToString()))
                    {
                        using (JsonTextReader reader = new JsonTextReader(sr))
                        {
                            deserializedObject = jsonExpandoSerializer.Deserialize<System.Dynamic.ExpandoObject>(reader);
                        }
                        sr.Close();
                    }
                    sw.Close();
                }
                using (StreamWriter writer = new StreamWriter(new FileStream(_path, System.IO.FileMode.Create, System.IO.FileAccess.Write)))
                {
                    try
                    {
                        yamlSerializer.Serialize(writer, deserializedObject);
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
