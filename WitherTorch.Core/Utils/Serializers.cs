using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using YamlDeserializerBuilder = YamlDotNet.Serialization.DeserializerBuilder;
using YamlSerializerBuilder = YamlDotNet.Serialization.SerializerBuilder;
using IYamlDeserializer = YamlDotNet.Serialization.IDeserializer;
using IYamlSerializer = YamlDotNet.Serialization.ISerializer;

namespace WitherTorch.Core.Utils
{
    public static class GlobalSerializers
    {
        private static JsonSerializer jsonSerializer;
        private static IYamlDeserializer yamlDeserializer;
        private static IYamlSerializer yamlSerializer;

        public static JsonSerializer JsonSerializer
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                if (jsonSerializer is null)
                {
                    jsonSerializer = JsonSerializer.CreateDefault();
                    jsonSerializer.Formatting = Formatting.Indented;
                }
                return jsonSerializer;
            }
        }

        public static IYamlDeserializer YamlDeserializer
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                return yamlDeserializer ?? (yamlDeserializer = new YamlDeserializerBuilder().Build());
            }
        }

        public static IYamlSerializer YamlSerializer
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                return yamlSerializer ?? (yamlSerializer = new YamlSerializerBuilder().Build());
            }
        }
    }
}
