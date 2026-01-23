using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProCohere.Avalonia.Converters;

/// <summary>
/// JSON converter that captures raw JSON (objects or arrays) as a string.
/// Used for JSONB columns that should be stored as string in the model
/// but come back from the database as actual JSON structures.
/// </summary>
public class RawJsonConverter : JsonConverter<string?>
{
    public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType == JsonToken.String)
            return reader.Value?.ToString();

        // For arrays or objects, capture the raw JSON
        var token = JToken.Load(reader);
        return token.ToString(Formatting.None);
    }

    public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        // Try to parse as JSON and write as raw JSON (not as escaped string)
        try
        {
            var token = JToken.Parse(value);
            token.WriteTo(writer);
        }
        catch
        {
            // If it's not valid JSON, write as string
            writer.WriteValue(value);
        }
    }
}
