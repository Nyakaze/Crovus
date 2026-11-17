using System.Buffers;
using System.Globalization;
using System.Text.Json;
using Crovus.Models;

namespace Crovus.RichPresence;

public sealed record RichPresenceMessage(string? Command, string? Event, string? Nonce, JsonElement Data)
{
    public bool IsError => string.Equals(Event, "ERROR", StringComparison.Ordinal);

    public bool IsReady => string.Equals(Command, "DISPATCH", StringComparison.Ordinal) &&
                           string.Equals(Event, "READY", StringComparison.Ordinal);
}

public static class RichPresenceJson
{
    public static byte[] Handshake(Snowflake applicationId) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("v", 1);
        writer.WriteString("client_id", applicationId.Value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndObject();
    });

    public static byte[] SetActivity(int processId, RichPresenceActivity? activity, string nonce) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteString("cmd", "SET_ACTIVITY");
        writer.WriteStartObject("args");
        writer.WriteNumber("pid", processId);

        if (activity is null)
            writer.WriteNull("activity");
        else
            WriteActivity(writer, activity);

        writer.WriteEndObject();
        writer.WriteString("nonce", nonce);
        writer.WriteEndObject();
    });

    public static RichPresenceMessage ParseMessage(ReadOnlySpan<byte> payload)
    {
        var reader = new Utf8JsonReader(payload);
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        return new RichPresenceMessage(
            Text(root, "cmd"),
            Text(root, "evt"),
            Text(root, "nonce"),
            root.TryGetProperty("data", out var data) ? data.Clone() : default);
    }

    public static (int? Code, string Message) ParseError(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
            return (null, "Discord closed the connection without a reason.");

        try
        {
            var reader = new Utf8JsonReader(payload);
            using var document = JsonDocument.ParseValue(ref reader);

            return ParseError(document.RootElement);
        }
        catch (JsonException)
        {
            return (null, System.Text.Encoding.UTF8.GetString(payload));
        }
    }

    public static (int? Code, string Message) ParseError(JsonElement data)
    {
        if (data.ValueKind is not JsonValueKind.Object)
            return (null, "Discord reported an error without details.");

        int? code = data.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var parsed)
            ? parsed
            : null;

        return (code, Text(data, "message") ?? "Discord reported an error without a message.");
    }

    public static RichPresenceUser? ParseReadyUser(JsonElement data)
    {
        if (data.ValueKind is not JsonValueKind.Object || !data.TryGetProperty("user", out var user) ||
            user.ValueKind is not JsonValueKind.Object)
            return null;

        if (!ulong.TryParse(Text(user, "id"), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return null;

        return new RichPresenceUser(new Snowflake(id), Text(user, "username") ?? string.Empty,
            Text(user, "global_name"), Text(user, "avatar"));
    }

    private static void WriteActivity(Utf8JsonWriter writer, RichPresenceActivity activity)
    {
        writer.WriteStartObject("activity");
        writer.WriteNumber("type", (int)activity.Type);
        WriteOptional(writer, "name", activity.Name);

        if (activity.StatusDisplay is { } display)
            writer.WriteNumber("status_display_type", (int)display);

        WriteOptional(writer, "details", activity.Details);
        WriteOptional(writer, "details_url", activity.DetailsUrl);
        WriteOptional(writer, "state", activity.State);
        WriteOptional(writer, "state_url", activity.StateUrl);

        if (activity.StartedAt is not null || activity.EndsAt is not null)
        {
            writer.WriteStartObject("timestamps");

            if (activity.StartedAt is { } start)
                writer.WriteNumber("start", start.ToUnixTimeMilliseconds());

            if (activity.EndsAt is { } end)
                writer.WriteNumber("end", end.ToUnixTimeMilliseconds());

            writer.WriteEndObject();
        }

        if (activity.Assets is { } assets &&
            (assets.LargeImage ?? assets.LargeText ?? assets.SmallImage ?? assets.SmallText) is not null)
        {
            writer.WriteStartObject("assets");
            WriteOptional(writer, "large_image", assets.LargeImage);
            WriteOptional(writer, "large_text", assets.LargeText);
            WriteOptional(writer, "small_image", assets.SmallImage);
            WriteOptional(writer, "small_text", assets.SmallText);
            writer.WriteEndObject();
        }

        if (activity.Party is { } party && (party.Id is not null || party.CurrentSize is not null))
        {
            writer.WriteStartObject("party");
            WriteOptional(writer, "id", party.Id);

            if (party.CurrentSize is { } current && party.MaxSize is { } max)
            {
                writer.WriteStartArray("size");
                writer.WriteNumberValue(current);
                writer.WriteNumberValue(max);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        if (activity.Buttons.Count > 0)
        {
            writer.WriteStartArray("buttons");

            foreach (var button in activity.Buttons)
            {
                writer.WriteStartObject();
                writer.WriteString("label", button.Label);
                writer.WriteString("url", button.Url);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        if (activity.Instance)
            writer.WriteBoolean("instance", true);

        writer.WriteEndObject();
    }

    private static void WriteOptional(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
            writer.WriteString(name, value);
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    private static byte[] Write(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>(256);

        using (var writer = new Utf8JsonWriter(buffer))
            write(writer);

        return buffer.WrittenSpan.ToArray();
    }
}
