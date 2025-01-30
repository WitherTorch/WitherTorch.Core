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

        /// <summary>
        /// 以指定的設定檔路徑，建立新的 <see cref="YamlPropertyFile"/> 物件
        /// </summary>
        /// <param name="path">設定檔的路徑</param>
        public YamlPropertyFile(string path) : base(path) { }

        /// <summary>
        /// 以指定的設定檔路徑，建立新的 <see cref="YamlPropertyFile"/> 物件，並決定是否持續監測 <paramref name="path"/> 所對應的檔案狀態
        /// </summary>
        /// <param name="path">設定檔的路徑</param>
        /// <param name="useFileWatcher">是否持續監測 <paramref name="path"/> 所對應的檔案狀態</param>
        public YamlPropertyFile(string path, bool useFileWatcher) : base(path, useFileWatcher) { }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
