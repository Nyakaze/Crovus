using System.Text.Json.Serialization;

namespace Crovus.Models;

public sealed record DiscordThreadMember
{
    public Snowflake? ThreadId { get; init; }

    public Snowflake? UserId { get; init; }

    public Snowflake? GuildId { get; init; }

    public DateTimeOffset? JoinedAt { get; init; }

    public int Flags { get; init; }

    public DiscordMember? Member { get; init; }

    [JsonIgnore]
    public DiscordUser? User => Member?.User;

    public DiscordThreadMember In(Snowflake guildId) => this with { GuildId = guildId };

    public DiscordThreadMember On(Snowflake threadId) => ThreadId is null ? this with { ThreadId = threadId } : this;

    public override string ToString() => $"{UserId} in {ThreadId}";
}
