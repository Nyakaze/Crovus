using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class GuildService : DiscordService
{
    public GuildService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Guild", logger, telemetry)
    {
    }

    public GuildService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public async Task<DiscordGuild> GetAsync(Snowflake guildId, bool withCounts = false,
        CancellationToken cancellationToken = default)
    {
        var guild = await TrackAsync(nameof(GetAsync), $"guild {guildId}",
            () => Rest.GetGuildAsync(guildId, withCounts, cancellationToken),
            loaded => $"Loaded guild {loaded.Name} ({loaded.Id})", LogLevel.Debug);

        Emit(new GuildFetched(guild.Id, guild.Name, guild.MemberCount ?? guild.ApproximateMemberCount ?? 0));

        return guild;
    }

    public Task<IReadOnlyList<DiscordChannel>> GetChannelsAsync(Snowflake guildId,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetChannelsAsync), $"channels of guild {guildId}",
            () => Rest.GetGuildChannelsAsync(guildId, cancellationToken),
            channels => $"Loaded {channels.Count} channels of guild {guildId}", LogLevel.Debug);

    public async Task<DiscordChannel?> FindChannelAsync(Snowflake guildId, string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var channels = await GetChannelsAsync(guildId, cancellationToken);

        return channels.FirstOrDefault(channel =>
            string.Equals(channel.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<DiscordUser> GetOwnerAsync(Snowflake guildId, CancellationToken cancellationToken = default)
    {
        var guild = await GetAsync(guildId, cancellationToken: cancellationToken);
        var owner = await Rest.GetGuildMemberAsync(guildId, guild.OwnerId, cancellationToken);

        return owner.User;
    }

    public async Task<DiscordPermissions> PermissionsOfAsync(Snowflake guildId, Snowflake userId,
        CancellationToken cancellationToken = default)
    {
        var guild = await GetAsync(guildId, cancellationToken: cancellationToken);
        var member = await Rest.GetGuildMemberAsync(guildId, userId, cancellationToken);

        return guild.PermissionsOf(member);
    }
}
