using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using WitherTorch.Core.Utils;

using YamlDotNet.System.Text.Json;

namespace WitherTorch.Core.Property
{
    /// <summary>
    /// 表示以 YAML 格式 (.yaml) 儲存的設定檔
    /// </summary>
    public class YamlPropertyFile : JsonPropertyFile
    {
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
            _jsonObject = YamlConverter.Deserialize<JsonObject>(reader.ReadToEnd(), GlobalSerializers.YamlDeserializer) ?? new JsonObject();
            reader.Close();
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
            SetFileWatching(false);
            using StreamWriter writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8);
            writer.Write(YamlConverter.Serialize(jsonObject, GlobalSerializers.YamlSerializer));
            writer.Flush();
            writer.Close();
            SetFileWatching(true);
        }
    }
}
