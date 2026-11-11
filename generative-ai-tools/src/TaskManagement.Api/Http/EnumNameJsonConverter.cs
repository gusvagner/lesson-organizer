using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManagement.Api.Http;

/// <summary>
/// Reads an enum only from a JSON string that is exactly one of its names (case-insensitive): numbers, numeric
/// strings and comma-separated combinations such as <c>"Todo, InProgress"</c> are rejected. Writes the name.
/// </summary>
public sealed class EnumNameJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        var name = EnumNames.Match(typeof(TEnum), text);
        return name is null
            ? throw new JsonException($"Expected one of {EnumNames.Allowed(typeof(TEnum))} as a string; got a {reader.TokenType} token.")
            : Enum.Parse<TEnum>(name);
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

public sealed class EnumNameJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(EnumNameJsonConverter<>).MakeGenericType(typeToConvert))!;
}
