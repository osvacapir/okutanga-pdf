using System.Text.Json;
using System.Text.Json.Serialization;

namespace OlondongeApp.Models.Dtos;

/// <summary>
/// Aceita bool em formatos heterogéneos da API: true/false, 1/0 e "1"/"0".
/// </summary>
public sealed class FlexibleBooleanJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.TryGetInt64(out var n) && n != 0,
            JsonTokenType.String => ParseString(reader.GetString()),
            _ => false,
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);

    private static bool ParseString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (bool.TryParse(value, out var parsedBool))
        {
            return parsedBool;
        }

        if (long.TryParse(value, out var parsedNumber))
        {
            return parsedNumber != 0;
        }

        return false;
    }
}
