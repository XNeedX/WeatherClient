using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherClient.Converters;

public class UnixEpochConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
            throw new JsonException($"Ожидался Number, получен {reader.TokenType}");

        var unixTimeSeconds = reader.GetInt64();

        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUnixTimeSeconds());
    }
}