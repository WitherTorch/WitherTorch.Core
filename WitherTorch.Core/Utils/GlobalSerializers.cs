using System.Runtime.CompilerServices;

using YamlDotNet.Serialization;

namespace WitherTorch.Core.Utils
{
    public static class GlobalSerializers
    {
        private static readonly IDeserializer yamlDeserializer;
        private static readonly ISerializer yamlSerializer;
        private static readonly ISerializer jsonSerializer;

        static GlobalSerializers()
        {
            yamlDeserializer = new DeserializerBuilder().Build();
            yamlSerializer = new SerializerBuilder().Build();
            jsonSerializer = new SerializerBuilder()
                .JsonCompatible()
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

        public static ISerializer JsonSerializer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => jsonSerializer;
        }
    }
}
