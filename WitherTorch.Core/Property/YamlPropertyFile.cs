using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Property;

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
    /// 以指定的設定檔路徑與建立模式，建立新的 <see cref="YamlPropertyFile"/> 物件
    /// </summary>
    /// <param name="path">設定檔的路徑</param>
    /// <param name="mode">設定檔案物件的建立模式</param>
    public YamlPropertyFile(string path, PropertyFileMode mode) : base(path, mode) { }

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
