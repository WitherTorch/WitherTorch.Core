using System.Runtime.CompilerServices;

using YamlDotNet.Serialization;
using YamlDotNet.System.Text.Json;

namespace WitherTorch.Core.Utils
{
    public static class GlobalSerializers
    {
        private static readonly IDeserializer yamlDeserializer;
        private static readonly ISerializer yamlSerializer;

        static GlobalSerializers()
        {
            yamlDeserializer = new DeserializerBuilder()
                .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                .Build();
            yamlSerializer = new SerializerBuilder()
                .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                .Build();
        }

        public static IDeserializer YamlDeserializer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => yamlDeserializer;
        }

        public static ISerializer YamlSerializer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => yamlSerializer;
        }
    }
}
