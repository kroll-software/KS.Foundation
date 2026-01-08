using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace KS.Foundation;

public class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReadValue(ref reader, options);

    private object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long l))
                    return l;
                return reader.GetDouble();
            case JsonTokenType.String:
                // Try DateTime
                if (reader.TryGetDateTime(out DateTime date))
                    return date;
                return reader.GetString();
            case JsonTokenType.StartObject:
                var dict = new Dictionary<string, object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string propName = reader.GetString();
                    reader.Read();
                    dict[propName] = ReadValue(ref reader, options);
                }
                return dict;
            case JsonTokenType.StartArray:
                var list = new List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ReadValue(ref reader, options));
                }
                return list.ToArray();
            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
    }
}

