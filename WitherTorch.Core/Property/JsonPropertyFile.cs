using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Property
{
    /// <summary>
    /// 表示以 JSON 格式 (.json) 儲存的設定檔
    /// </summary>
    public class JsonPropertyFile : PropertyFileBase<JsonNode>
    {
        private JsonObject? _jsonObject = null;

        /// <summary>
        /// 以指定的設定檔路徑，建立新的 <see cref="JsonPropertyFile"/> 物件
        /// </summary>
        /// <param name="path">設定檔的路徑</param>
        public JsonPropertyFile(string path) : base(path) { }

        /// <summary>
        /// 以指定的設定檔路徑，建立新的 <see cref="JsonPropertyFile"/> 物件，並決定是否持續監測 <paramref name="path"/> 所對應的檔案狀態
        /// </summary>
        /// <param name="path">設定檔的路徑</param>
        /// <param name="useFileWatcher">是否持續監測 <paramref name="path"/> 所對應的檔案狀態</param>
        public JsonPropertyFile(string path, bool useFileWatcher) : base(path, useFileWatcher) { }

        /// <inheritdoc/>
        protected override void LoadCore(Stream? stream)
        {
            if (stream is null)
            {
                LoadCore(new JsonObject());
                return;
            }
            JsonObject? obj;
            try
            {
                obj = JsonNode.Parse(stream) as JsonObject;
            }
            catch (Exception)
            {
                obj = null;
            }
            LoadCore(obj);
        }

        /// <inheritdoc/>
        protected void LoadCore(JsonObject? obj)
        {
            _jsonObject = obj ?? new JsonObject();
        }

        /// <inheritdoc/>
        protected override void UnloadCore()
        {
            _jsonObject = null;
        }

        /// <inheritdoc/>
        protected override JsonNode? GetValueCore(string key)
        {
            return JsonPathHelper.GetNodeFromPath(_jsonObject, key);
        }

        /// <inheritdoc/>
        protected override bool SetValueCore(string key, JsonNode value)
        {
            JsonPathHelper.SetNodeFromPath(_jsonObject, key, value);
            return true;
        }

        /// <inheritdoc/>
        protected override bool RemoveValueCore(string key)
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
                        return _obj.Remove(key);
                    case JsonArray _arr:
                        return _arr.Remove(node);
                }
            }
            return false;
        }

        /// <inheritdoc/>
        protected override void SaveCore(Stream stream)
            => SaveCore(stream, _jsonObject ?? new JsonObject());

        /// <inheritdoc/>
        protected virtual void SaveCore(Stream stream, JsonObject obj)
        {
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true });
            obj.WriteTo(writer);
            writer.Flush();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            Load(force: false);
            JsonObject jsonObject = _jsonObject ?? new JsonObject();
            return jsonObject.ToString();
        }
    }
}
