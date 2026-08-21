using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class ChannelFactory
{
    private readonly Dictionary<(Snowflake Id, PermissionOverwriteType Type), Overwrite> _overwrites = [];

    private string? _name;
    private ChannelType? _type;
    private string? _topic;
    private int? _position;
    private bool? _nsfw;
    private int? _slowmode;
    private int? _bitrate;
    private int? _userLimit;
    private Snowflake? _parentId;
    private ThreadArchiveDuration? _defaultArchiveDuration;
    private bool _overwritesTouched;

    public static ChannelFactory Text(string name) => Of(name, ChannelType.GuildText);

    public static ChannelFactory Voice(string name) => Of(name, ChannelType.GuildVoice);

    public static ChannelFactory Category(string name) => Of(name, ChannelType.GuildCategory);

    public static ChannelFactory Announcement(string name) => Of(name, ChannelType.GuildAnnouncement);

    public static ChannelFactory Stage(string name) => Of(name, ChannelType.GuildStageVoice);

    public static ChannelFactory Forum(string name) => Of(name, ChannelType.GuildForum);

    public static ChannelFactory Media(string name) => Of(name, ChannelType.GuildMedia);

    public static ChannelFactory Of(string name, ChannelType type) =>
        new ChannelFactory { _type = type }.WithName(name);

    public static ChannelFactory Modify() => new();

    public static ChannelFactory From(DiscordChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        return new ChannelFactory
        {
            _name = channel.Name,
            _type = channel.Type,
            _parentId = channel.ParentId
        };
    }

    public ChannelFactory WithName(string name)
    {
        _name = Limit.Required(name, DiscordLimits.ChannelName, nameof(name));
        return this;
    }

    public ChannelFactory WithType(ChannelType type)
    {
        _type = type;
        return this;
    }

    public ChannelFactory WithTopic(string? topic)
    {
        _topic = topic;
        return this;
    }

    public ChannelFactory WithPosition(int position)
    {
        _position = Limit.Range(position, 0, int.MaxValue, nameof(position));
        return this;
    }

    public ChannelFactory AsNsfw(bool nsfw = true)
    {
        _nsfw = nsfw;
        return this;
    }

    public ChannelFactory WithSlowmode(TimeSpan slowmode) => WithSlowmode((int)slowmode.TotalSeconds);

    public ChannelFactory WithSlowmode(int seconds)
    {
        _slowmode = Limit.Range(seconds, 0, DiscordLimits.MaxSlowmodeSeconds, nameof(seconds));
        return this;
    }

    public ChannelFactory WithBitrate(int bitrate)
    {
        _bitrate = Limit.Range(bitrate, DiscordLimits.MinBitrate, DiscordLimits.MaxBitrate, nameof(bitrate));
        return this;
    }

    public ChannelFactory WithUserLimit(int userLimit)
    {
        var max = _type is ChannelType.GuildStageVoice
            ? DiscordLimits.MaxStageUserLimit
            : DiscordLimits.MaxVoiceUserLimit;

        _userLimit = Limit.Range(userLimit, 0, max, nameof(userLimit));

        return this;
    }

    public ChannelFactory InCategory(Snowflake? categoryId)
    {
        _parentId = categoryId;
        return this;
    }

    public ChannelFactory InCategory(DiscordChannel category)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.Type is not ChannelType.GuildCategory)
            throw new ArgumentException($"Channel {category.Id} is a {category.Type}, not a category.",
                nameof(category));

        _parentId = category.Id;

        return this;
    }

    public ChannelFactory WithDefaultArchiveDuration(ThreadArchiveDuration duration)
    {
        _defaultArchiveDuration = duration;
        return this;
    }

    public ChannelFactory AllowRole(Snowflake roleId, DiscordPermissions permissions) =>
        Merge(roleId, PermissionOverwriteType.Role, permissions, DiscordPermissions.None);

    public ChannelFactory DenyRole(Snowflake roleId, DiscordPermissions permissions) =>
        Merge(roleId, PermissionOverwriteType.Role, DiscordPermissions.None, permissions);

    public ChannelFactory AllowMember(Snowflake userId, DiscordPermissions permissions) =>
        Merge(userId, PermissionOverwriteType.Member, permissions, DiscordPermissions.None);

    public ChannelFactory DenyMember(Snowflake userId, DiscordPermissions permissions) =>
        Merge(userId, PermissionOverwriteType.Member, DiscordPermissions.None, permissions);

    public ChannelFactory WithOverwrite(DiscordPermissionOverwrite overwrite)
    {
        ArgumentNullException.ThrowIfNull(overwrite);

        return Merge(overwrite.Id, overwrite.Type, overwrite.Allow, overwrite.Deny);
    }

    public ChannelFactory ClearOverwrites()
    {
        _overwrites.Clear();
        _overwritesTouched = true;

        return this;
    }

    public ChannelCreateRequest Build()
    {
        if (_name is not { } name)
            throw new InvalidOperationException("A channel cannot be created without a name.");

        if (_type is not { } type)
            throw new InvalidOperationException("A channel cannot be created without a type.");

        if (type is ChannelType.Dm or ChannelType.GroupDm or ChannelType.GuildDirectory)
            throw new InvalidOperationException($"A {type} channel cannot be created through the guild endpoint.");

        return new ChannelCreateRequest(name, type)
        {
            Topic = ValidatedTopic(type),
            Position = _position,
            Nsfw = _nsfw,
            RateLimitPerUser = _slowmode,
            Bitrate = _bitrate,
            UserLimit = _userLimit,
            ParentId = _parentId,
            DefaultAutoArchiveDuration = _defaultArchiveDuration,
            PermissionOverwrites = BuildOverwrites()
        };
    }

    public ChannelModifyRequest BuildModify()
    {
        var request = new ChannelModifyRequest
        {
            Name = _name,
            Type = _type,
            Topic = ValidatedTopic(_type),
            Position = _position,
            Nsfw = _nsfw,
            RateLimitPerUser = _slowmode,
            Bitrate = _bitrate,
            UserLimit = _userLimit,
            ParentId = _parentId,
            DefaultAutoArchiveDuration = _defaultArchiveDuration,
            PermissionOverwrites = BuildOverwrites()
        };

        if (request.IsEmpty)
            throw new InvalidOperationException("The modification would not change anything.");

        return request;
    }

    public Task<DiscordChannel> CreateAsync(IDiscordRest rest, Snowflake guildId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.CreateChannelAsync(guildId, Build(), reason, cancellationToken);
    }

    public Task<DiscordChannel> ApplyAsync(IDiscordRest rest, Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.ModifyChannelAsync(channelId, BuildModify(), reason, cancellationToken);
    }

    private ChannelFactory Merge(Snowflake id, PermissionOverwriteType type, DiscordPermissions allow,
        DiscordPermissions deny)
    {
        var key = (id, type);
        var current = _overwrites.GetValueOrDefault(key);

        _overwrites[key] = new Overwrite(
            (current.Allow | allow) & ~deny,
            (current.Deny | deny) & ~allow);

        _overwritesTouched = true;

        return this;
    }

    private IReadOnlyList<DiscordPermissionOverwrite>? BuildOverwrites()
    {
        if (!_overwritesTouched)
            return null;

        return _overwrites
            .Select(entry => new DiscordPermissionOverwrite(entry.Key.Id, entry.Key.Type, entry.Value.Allow,
                entry.Value.Deny))
            .ToArray();
    }

    private string? ValidatedTopic(ChannelType? type)
    {
        var max = type is ChannelType.GuildForum or ChannelType.GuildMedia
            ? DiscordLimits.ForumTopic
            : DiscordLimits.ChannelTopic;

        return Limit.Text(_topic, max, "topic");
    }

    private readonly record struct Overwrite(DiscordPermissions Allow, DiscordPermissions Deny);
}
