using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Property
{
    /// <summary>
    /// 表示以 YAML 格式 (.yaml) 儲存的設定檔
    /// </summary>
    public class YamlPropertyFile : JsonPropertyFile
    {
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DynamicJsonConverter() },
            WriteIndented = true
        };

        public YamlPropertyFile(string path) : base(path) { }

        public YamlPropertyFile(string path, bool useFileWatcher) : base(path, useFileWatcher) { }

        protected override void LoadCore(Stream? stream)
        {
            if (stream is null)
            {
                LoadCore(new JsonObject());
                return;
            }
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, bufferSize: 4096, leaveOpen: true);
            object? graph = GlobalSerializers.YamlDeserializer.Deserialize(reader);
            LoadCore(JsonNode.Parse(GlobalSerializers.JsonSerializer.Serialize(graph)) as JsonObject);
        }

        protected override void SaveCore(Stream stream, JsonObject obj)
        {
            dynamic? graph = JsonSerializer.Deserialize<dynamic>(obj, _serializerOptions);
            if (graph is null)
                return;
            using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
            GlobalSerializers.YamlSerializer.Serialize(writer, graph);
            writer.Flush();
        }
    }
}
