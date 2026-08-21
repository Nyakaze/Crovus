using System.Text.Json;
using Crovus.Models;

namespace Crovus.Json;

internal static class JsonElementExtensions
{
    public static JsonElement? Property(this JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind is not JsonValueKind.Null ? value : null;

    public static string RequireString(this JsonElement element, string name) =>
        element.Property(name)?.GetString() ??
        throw new JsonException($"Expected a string property named '{name}'.");

    public static string? StringOrNull(this JsonElement element, string name) =>
        element.Property(name)?.GetString();

    public static Snowflake RequireSnowflake(this JsonElement element, string name) =>
        element.SnowflakeOrNull(name) ??
        throw new JsonException($"Expected a snowflake property named '{name}'.");

    public static Snowflake? SnowflakeOrNull(this JsonElement element, string name) =>
        element.Property(name) is { } value
            ? new Snowflake(value.ValueKind is JsonValueKind.String
                ? ulong.Parse(value.GetString()!)
                : value.GetUInt64())
            : null;

    public static int RequireInt32(this JsonElement element, string name) =>
        element.Int32OrNull(name) ?? throw new JsonException($"Expected a number property named '{name}'.");

    public static int? Int32OrNull(this JsonElement element, string name) =>
        element.Property(name)?.GetInt32();

    public static int? IntegerOrNull(this JsonElement element, string name) => element.Property(name) switch
    {
        { ValueKind: JsonValueKind.Number } value => value.GetInt32(),
        { ValueKind: JsonValueKind.String } value when int.TryParse(value.GetString(), out var parsed) => parsed,
        _ => null
    };

    public static bool Flag(this JsonElement element, string name) =>
        element.Property(name) is { ValueKind: JsonValueKind.True };

    public static T? Deserialize<T>(this JsonElement element, string name, JsonSerializerOptions options) =>
        element.Property(name) is { } value ? value.Deserialize<T>(options) : default;

    public static DateTimeOffset? TimestampOrNull(this JsonElement element, string name) =>
        element.Property(name) is { } value && value.TryGetDateTimeOffset(out var timestamp)
            ? timestamp
            : null;

    public static IReadOnlyList<Snowflake> SnowflakeList(this JsonElement element, string name)
    {
        if (element.Property(name) is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var values = new List<Snowflake>();

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.String && ulong.TryParse(item.GetString(), out var parsed))
                values.Add(new Snowflake(parsed));
            else if (item.ValueKind is JsonValueKind.Number)
                values.Add(new Snowflake(item.GetUInt64()));
        }

        return values;
    }

    public static IReadOnlyList<string> StringList(this JsonElement element, string name)
    {
        if (element.Property(name) is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var values = new List<string>();

        foreach (var item in array.EnumerateArray())
            if (item.GetString() is { } text)
                values.Add(text);

        return values;
    }

    public static IReadOnlyList<T> DeserializeList<T>(this JsonElement element, string name,
        JsonSerializerOptions options) =>
        element.Property(name) is { ValueKind: JsonValueKind.Array } value
            ? value.Deserialize<List<T>>(options) ?? []
            : [];
}
