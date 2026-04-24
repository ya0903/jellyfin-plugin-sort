using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.RatingSort.Ratings;

internal sealed class MdbListTitleResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("ids")]
    public MdbListIds? Ids { get; set; }

    [JsonPropertyName("ratings")]
    public List<MdbListRating> Ratings { get; set; } = [];
}

internal sealed class MdbListIds
{
    [JsonPropertyName("tmdb")]
    [JsonConverter(typeof(NullableIntLenientConverter))]
    public int? Tmdb { get; set; }

    [JsonPropertyName("imdb")]
    public string? Imdb { get; set; }
}

internal sealed class MdbListRating
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(NullableDoubleLenientConverter))]
    public double? Value { get; set; }

    [JsonPropertyName("score")]
    [JsonConverter(typeof(NullableDoubleLenientConverter))]
    public double? Score { get; set; }

    [JsonPropertyName("votes")]
    [JsonConverter(typeof(NullableIntLenientConverter))]
    public int? Votes { get; set; }
}

internal sealed class MdbListLookupResult
{
    public int? StatusCode { get; init; }

    public bool IsRateLimited { get; init; }

    public int? RateLimitRemaining { get; init; }

    public DateTimeOffset? RateLimitResetUtc { get; init; }

    public MdbListTitleResponse? Data { get; init; }
}

internal sealed class NullableDoubleLenientConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetDouble(out var d) ? d : null,
            JsonTokenType.String => TryParse(reader.GetString()),
            JsonTokenType.Null => null,
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    private static double? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "n/a", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "na", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }
}

internal sealed class NullableIntLenientConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.TryGetInt32(out var i) ? i : null,
            JsonTokenType.String => TryParse(reader.GetString()),
            JsonTokenType.Null => null,
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    private static int? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "n/a", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "na", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? Convert.ToInt32(Math.Round(d, MidpointRounding.AwayFromZero))
            : null;
    }
}
