using System;
using System.Text.Json;

namespace KS.Foundation;

public static class JsonExtensions
{
    public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        Converters = { new ObjectToInferredTypesConverter() },
        PropertyNameCaseInsensitive = true
    };

    public static string ToJson(this object value)
    {
        return JsonSerializer.Serialize(value, DefaultOptions);
    }

    public static T FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }
}
