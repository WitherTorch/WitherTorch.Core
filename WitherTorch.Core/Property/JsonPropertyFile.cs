using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Property
{
    /// <summary>
    /// 表示以 JSON 格式 (.json) 儲存的設定檔
    /// </summary>
    public class JsonPropertyFile : AbstractPropertyFile
    {
        protected bool _create;
        protected JsonObject? _jsonObject;

        /// <inheritdoc/>
        public JsonNode? this[string key]
        {
            get
            {
                if (key is null)
                    return null;
                Initialize();
                return JsonPathHelper.GetNodeFromPath(_jsonObject, key);
            }
            set
            {
                if (key is null)
                    return;
                Initialize();
                if (value is null)
                {
                    JsonNode? node = JsonPathHelper.GetNodeFromPath(_jsonObject, key);
                    if (node is not null)
                    {
                        switch (node.Parent)
                        {
                            case JsonObject _obj:
                                int lastIndexOf = key.LastIndexOf('.');
                                if (lastIndexOf >= 0)
                                    key = key.Substring(lastIndexOf + 1);
                                _obj.Remove(key);
                                break;
                            case JsonArray _arr:
                                _arr.Remove(node);
                                break;
                        }
                        MarkAsDirty();
                    }
                    return;
                }
                JsonPathHelper.SetNodeFromPath(_jsonObject, key, value);
                MarkAsDirty();
            }
        }

        public JsonPropertyFile(string path, bool create = true, bool ignoreLazyRequest = false) : base(path)
        {
            _create = create;
            if (!WTCore.UseLazyLoadingOnPropertyFiles || ignoreLazyRequest)
                Initialize();
        }

        protected override void Initialize(bool isDirty) => Reload();

        protected override void ClearCache()
        {
            base.ClearCache();
            _jsonObject = null;
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
            using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            _jsonObject = JsonNode.Parse(stream) as JsonObject ?? new JsonObject();
            stream.Close();
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
            using Utf8JsonWriter writer = new Utf8JsonWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read),
                new JsonWriterOptions() { Indented = true });
            jsonObject.WriteTo(writer);
            writer.Flush();
            SetFileWatching(true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            Initialize();
            return _jsonObject?.ToString() ?? string.Empty;
        }
    }
}
