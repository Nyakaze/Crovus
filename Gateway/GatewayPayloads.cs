using System.Text.Json.Serialization;
using Crovus.Json;
using Crovus.Models;

namespace Crovus.Gateway;

internal sealed record PresenceActivityPayload(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] int Type,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("state")] string? State);

internal sealed record PresenceUpdatePayload(
    [property: JsonPropertyName("since")] long? Since,
    [property: JsonPropertyName("activities")] IReadOnlyList<PresenceActivityPayload> Activities,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("afk")] bool Afk)
{
    public static PresenceUpdatePayload From(PresenceUpdate presence)
    {
        ArgumentNullException.ThrowIfNull(presence);

        return new PresenceUpdatePayload(
            presence.IdleSince?.ToUnixTimeMilliseconds(),
            presence.Activities
                .Select(activity => new PresenceActivityPayload(activity.Name, (int)activity.Type, activity.Url,
                    activity.State))
                .ToArray(),
            UserStatusConverter.Format(presence.Status),
            presence.Afk);
    }
}

internal sealed record RequestGuildMembersPayload
{
    [JsonPropertyName("guild_id")]
    public required string GuildId { get; init; }

    [JsonPropertyName("query")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Query { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("presences")]
    public bool Presences { get; init; }

    [JsonPropertyName("user_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? UserIds { get; init; }

    [JsonPropertyName("nonce")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Nonce { get; init; }

    public static RequestGuildMembersPayload From(GuildMembersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RequestGuildMembersPayload
        {
            GuildId = request.GuildId.ToString(),
            Query = request.IsTargeted ? null : request.Query ?? string.Empty,
            Limit = request.IsTargeted ? 0 : request.Limit,
            Presences = request.WithPresences,
            UserIds = request.IsTargeted
                ? request.UserIds.Select(userId => userId.ToString()).ToArray()
                : null,
            Nonce = request.Nonce
        };
    }
}

internal sealed record GatewayOutboundPayload(
    [property: JsonPropertyName("op")] int Op,
    [property: JsonPropertyName("d")] object? Data);

internal sealed record ConnectionProperties(
    [property: JsonPropertyName("os")] string Os,
    [property: JsonPropertyName("browser")] string Browser,
    [property: JsonPropertyName("device")] string Device);

internal sealed record IdentifyPayload
{
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    [JsonPropertyName("intents")]
    public required int Intents { get; init; }

    [JsonPropertyName("properties")]
    public required ConnectionProperties Properties { get; init; }

    [JsonPropertyName("large_threshold")]
    public int LargeThreshold { get; init; }

    [JsonPropertyName("shard")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? Shard { get; init; }

    [JsonPropertyName("presence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Presence { get; init; }
}

internal sealed record ResumePayload(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("seq")] int Sequence);
