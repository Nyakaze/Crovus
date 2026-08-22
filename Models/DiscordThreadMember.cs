using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public sealed record DiscordThreadMember : IBoundEntity
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

    private EntityBinding _binding;

    public DiscordThreadMember Bind(ICrovusContext context)
    {
        var bound = this with { Member = Member?.Bind(context) };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}
