using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class ChannelService : DiscordService
{
    public ChannelService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Channel", logger, telemetry)
    {
    }

    public ChannelService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<DiscordChannel> GetAsync(Snowflake channelId, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAsync), $"channel {channelId}",
            () => Rest.GetChannelAsync(channelId, cancellationToken),
            channel => $"Loaded channel {channel.Name} ({channel.Id})", LogLevel.Debug);

    public Task<DiscordChannel> CreateAsync(Snowflake guildId, ChannelCreateRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(CreateAsync), $"guild {guildId}",
            () => Rest.CreateChannelAsync(guildId, request, reason, cancellationToken),
            channel => $"Created {channel.Type} channel {channel.Name} ({channel.Id}) in guild {guildId}" +
                       Because(reason));
    }

    public Task<DiscordChannel> CreateAsync(Snowflake guildId, ChannelFactory channel, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(channel);

        return CreateAsync(guildId, channel.Build(), reason, cancellationToken);
    }

    public Task<DiscordChannel> CreateAsync(Snowflake guildId, string name, ChannelType type,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Of(name, type), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreateTextAsync(Snowflake guildId, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Text(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreateVoiceAsync(Snowflake guildId, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Voice(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreateCategoryAsync(Snowflake guildId, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Category(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreateAnnouncementAsync(Snowflake guildId, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Announcement(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreateStageAsync(Snowflake guildId, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Stage(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreateForumAsync(Snowflake guildId, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Forum(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> CreateMediaAsync(Snowflake guildId, string name,
        Action<ChannelFactory>? configure = null, string? reason = null,
        CancellationToken cancellationToken = default) =>
        CreateAsync(guildId, Compose(ChannelFactory.Media(name), configure), reason, cancellationToken);

    public Task<DiscordChannel> ModifyAsync(Snowflake channelId, ChannelModifyRequest request, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(ModifyAsync), $"channel {channelId}",
            () => Rest.ModifyChannelAsync(channelId, request, reason, cancellationToken),
            channel => $"Modified channel {channel.Name} ({channel.Id}){Because(reason)}");
    }

    public Task<DiscordChannel> ModifyAsync(Snowflake channelId, Action<ChannelFactory> configure,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var factory = ChannelFactory.Modify();
        configure(factory);

        return ModifyAsync(channelId, factory.BuildModify(), reason, cancellationToken);
    }

    public Task<DiscordChannel> RenameAsync(Snowflake channelId, string name, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.WithName(name), reason, cancellationToken);

    public Task<DiscordChannel> MoveAsync(Snowflake channelId, Snowflake? categoryId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.InCategory(categoryId), reason, cancellationToken);

    public Task<DiscordChannel> ReorderAsync(Snowflake channelId, int position, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.WithPosition(position), reason, cancellationToken);

    public Task<DiscordChannel> SetTopicAsync(Snowflake channelId, string? topic, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.WithTopic(topic), reason, cancellationToken);

    public Task<DiscordChannel> SetSlowmodeAsync(Snowflake channelId, TimeSpan slowmode, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.WithSlowmode(slowmode), reason, cancellationToken);

    public Task<DiscordChannel> SetNsfwAsync(Snowflake channelId, bool nsfw = true, string? reason = null,
        CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.AsNsfw(nsfw), reason, cancellationToken);

    public Task<DiscordChannel> GrantAsync(Snowflake channelId, Snowflake roleId, DiscordPermissions permissions,
        string? reason = null, CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.AllowRole(roleId, permissions), reason, cancellationToken);

    public Task<DiscordChannel> RevokeAsync(Snowflake channelId, Snowflake roleId, DiscordPermissions permissions,
        string? reason = null, CancellationToken cancellationToken = default) =>
        ModifyAsync(channelId, channel => channel.DenyRole(roleId, permissions), reason, cancellationToken);

    public Task DeleteAsync(Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAsync), $"channel {channelId}",
            () => Rest.DeleteChannelAsync(channelId, reason, cancellationToken),
            $"Deleted channel {channelId}{Because(reason)}");

    private static ChannelCreateRequest Compose(ChannelFactory factory, Action<ChannelFactory>? configure)
    {
        configure?.Invoke(factory);
        return factory.Build();
    }
}
