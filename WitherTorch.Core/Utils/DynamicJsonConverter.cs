using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WitherTorch.Core.Utils
{
    // From https://gist.github.com/doggy8088/995a28b2655ec9529414c3df18aaa28e
    internal sealed class DynamicJsonConverter : JsonConverter<dynamic>
    {
        public override dynamic? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long l))
                {
                    return l;
                }

                return reader.GetDouble();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                if (reader.TryGetDateTime(out DateTime datetime))
                {
                    return datetime;
                }

                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using JsonDocument documentV = JsonDocument.ParseValue(ref reader);
                return ReadObject(documentV.RootElement);
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                using JsonDocument documentV = JsonDocument.ParseValue(ref reader);
                return ReadList(documentV.RootElement);
            }

            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone();
        }

        private object? ReadObject(JsonElement jsonElement)
        {
            IDictionary<string, object?> expandoObject = new ExpandoObject();
            foreach (var obj in jsonElement.EnumerateObject())
            {
                var k = obj.Name;
                var value = ReadValue(obj.Value);
                if (value is null)
                    continue;
                expandoObject[k] = value;
            }
            return expandoObject;
        }

        private object? ReadValue(JsonElement jsonElement)
        {
            object? result = null;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    result = ReadObject(jsonElement);
                    break;
                case JsonValueKind.Array:
                    result = ReadList(jsonElement);
                    break;
                case JsonValueKind.String:
                    result = jsonElement.GetString();
                    break;
                case JsonValueKind.Number:
                    if (jsonElement.TryGetDecimal(out decimal d))
                    {
                        result = d;
                    }
                    else if (jsonElement.TryGetInt64(out long l))
                    {
                        result = l;
                    }
                    else
                    {
                        result = 0;
                    }
                    break;
                case JsonValueKind.True:
                    result = true;
                    break;
                case JsonValueKind.False:
                    result = false;
                    break;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    result = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return result;
        }

        private object? ReadList(JsonElement jsonElement)
        {
            IList<object> list = new List<object>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                var obj = ReadValue(item);
                if (obj is null)
                    continue;
                list.Add(obj);
            }
            return list.Count == 0 ? null : list;
        }

        public override void Write(Utf8JsonWriter writer, dynamic value, JsonSerializerOptions options)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.typecode
            switch (Type.GetTypeCode((Type)value.GetType()))
            {
                case TypeCode.Boolean:
                    writer.WriteBooleanValue(Convert.ToBoolean(value));
                    break;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    writer.WriteNumberValue(Convert.ToInt64(value));
                    break;
                case TypeCode.Decimal:
                    writer.WriteNumberValue(Convert.ToDecimal(value));
                    break;
                case TypeCode.Char:
                case TypeCode.Empty:
                case TypeCode.String:
                    writer.WriteStringValue(Convert.ToString(value));
                    break;
                case TypeCode.DateTime:
                    writer.WriteStringValue(Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss"));
                    break;
                case TypeCode.DBNull:
                    writer.WriteNullValue();
                    break;
                default:
                    writer.WriteRawValue(JsonSerializer.Serialize(value, new JsonSerializerOptions() { WriteIndented = true }));
                    break;
            }
        }
    }
}
