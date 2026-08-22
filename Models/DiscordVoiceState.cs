using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public sealed record DiscordVoiceState : IBoundEntity
{
    public Snowflake? GuildId { get; init; }

    public Snowflake? ChannelId { get; init; }

    public required Snowflake UserId { get; init; }

    public DiscordMember? Member { get; init; }

    public string SessionId { get; init; } = string.Empty;

    public bool Deaf { get; init; }

    public bool Mute { get; init; }

    public bool SelfDeaf { get; init; }

    public bool SelfMute { get; init; }

    public bool SelfStream { get; init; }

    public bool SelfVideo { get; init; }

    public bool Suppress { get; init; }

    public DateTimeOffset? RequestToSpeakAt { get; init; }

    [JsonIgnore]
    public bool IsConnected => ChannelId is not null;

    [JsonIgnore]
    public bool IsSilenced => Deaf || Mute;

    [JsonIgnore]
    public bool IsSelfSilenced => SelfDeaf || SelfMute;

    [JsonIgnore]
    public bool IsBroadcasting => SelfStream || SelfVideo;

    [JsonIgnore]
    public bool WantsToSpeak => RequestToSpeakAt is not null;

    public DiscordVoiceState In(Snowflake guildId) => this with { GuildId = guildId };

    public override string ToString() =>
        ChannelId is { } channelId ? $"{UserId} in {channelId}" : $"{UserId} disconnected";

    private EntityBinding _binding;

    public DiscordVoiceState Bind(ICrovusContext context)
    {
        var bound = this with { Member = Member?.Bind(context) };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}
