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
        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DynamicJsonConverter() },
            WriteIndented = true
        };

        public YamlPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false) : base(path, create, ignoreLazyRequest)
        {
        }

        protected override void Reload(bool isDirty)
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                if (_create)
                    _jsonObject ??= new JsonObject();
                return;
            }
            using StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.UTF8);
            object? graph = GlobalSerializers.YamlDeserializer.Deserialize(reader);
            reader.Close();
            if (graph is null)
            {
                _jsonObject = new JsonObject();
                return;
            }
            _jsonObject = JsonNode.Parse(GlobalSerializers.JsonSerializer.Serialize(graph)) as JsonObject ?? new JsonObject();
        }

        protected override void Save(bool isDirty, bool force)
        {
            string path = FilePath;
            if (string.IsNullOrEmpty(path) || !force && !isDirty)
                return;
            if (force)
                Initialize();
            JsonObject? jsonObject = _jsonObject;
            if (jsonObject is null)
                return;
            dynamic? obj = JsonSerializer.Deserialize<dynamic>(jsonObject, serializerOptions);
            if (obj is null)
                return;
            SetFileWatching(false);
            using StreamWriter writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);
            GlobalSerializers.YamlSerializer.Serialize(writer, obj);
            writer.Flush();
            writer.Close();
            SetFileWatching(true);
        }
    }
}
