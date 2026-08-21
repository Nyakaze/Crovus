using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class ThreadFactory
{
    private readonly List<Snowflake> _appliedTags = [];

    private string _name;
    private ChannelType _type = ChannelType.PublicThread;
    private ThreadArchiveDuration? _archiveDuration;
    private bool? _invitable;
    private int? _slowmode;
    private MessageCreateRequest? _starter;

    private ThreadFactory(string name) =>
        _name = Limit.Required(name, DiscordLimits.ThreadName, nameof(name));

    public static ThreadFactory Public(string name) => new(name) { _type = ChannelType.PublicThread };

    public static ThreadFactory Private(string name) => new(name) { _type = ChannelType.PrivateThread };

    public static ThreadFactory Announcement(string name) => new(name) { _type = ChannelType.AnnouncementThread };

    public static ThreadFactory ForumPost(string name) => new(name) { _type = ChannelType.PublicThread };

    public static ThreadFactory Of(string name, ChannelType type) => new(name) { _type = type };

    public bool IsPost => _starter is not null;

    public ThreadFactory WithName(string name)
    {
        _name = Limit.Required(name, DiscordLimits.ThreadName, nameof(name));
        return this;
    }

    public ThreadFactory WithType(ChannelType type)
    {
        if (type is not (ChannelType.PublicThread or ChannelType.PrivateThread or ChannelType.AnnouncementThread))
            throw new ArgumentException($"{type} is not a thread type.", nameof(type));

        _type = type;

        return this;
    }

    public ThreadFactory WithArchiveDuration(ThreadArchiveDuration duration)
    {
        _archiveDuration = duration;
        return this;
    }

    public ThreadFactory AsInvitable(bool invitable = true)
    {
        _invitable = invitable;
        return this;
    }

    public ThreadFactory WithSlowmode(TimeSpan slowmode) => WithSlowmode((int)slowmode.TotalSeconds);

    public ThreadFactory WithSlowmode(int seconds)
    {
        _slowmode = Limit.Range(seconds, 0, DiscordLimits.MaxSlowmodeSeconds, nameof(seconds));
        return this;
    }

    public ThreadFactory WithStarterMessage(MessageCreateRequest message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _starter = message;

        return this;
    }

    public ThreadFactory WithStarterMessage(MessageFactory message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return WithStarterMessage(message.Build());
    }

    public ThreadFactory WithStarterMessage(Action<MessageFactory> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var message = MessageFactory.Create();
        configure(message);

        return WithStarterMessage(message.Build());
    }

    public ThreadFactory WithStarterMessage(string content) =>
        WithStarterMessage(MessageFactory.Create(content).Build());

    public ThreadFactory WithTags(params Snowflake[] tagIds) => WithTags((IEnumerable<Snowflake>)tagIds);

    public ThreadFactory WithTags(IEnumerable<Snowflake> tagIds)
    {
        ArgumentNullException.ThrowIfNull(tagIds);

        _appliedTags.Clear();
        _appliedTags.AddRange(tagIds);

        Limit.Count(_appliedTags.Count, DiscordLimits.ForumAppliedTags, nameof(tagIds));

        return this;
    }

    public ThreadCreateRequest Build() =>
        new(_name)
        {
            Type = _type,
            AutoArchiveDuration = _archiveDuration,
            Invitable = _type is ChannelType.PrivateThread ? _invitable : null,
            RateLimitPerUser = _slowmode,
            Message = _starter,
            AppliedTags = _appliedTags.Count == 0 ? null : _appliedTags.ToArray()
        };

    public ThreadFromMessageRequest BuildFromMessage()
    {
        if (_starter is not null)
            throw new InvalidOperationException(
                "A thread started from an existing message cannot carry its own starter message.");

        return new ThreadFromMessageRequest(_name)
        {
            AutoArchiveDuration = _archiveDuration,
            RateLimitPerUser = _slowmode
        };
    }

    public Task<DiscordChannel> StartAsync(IDiscordRest rest, Snowflake channelId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.StartThreadAsync(channelId, Build(), reason, cancellationToken);
    }

    public Task<DiscordChannel> StartFromAsync(IDiscordRest rest, Snowflake channelId, Snowflake messageId,
        string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        return rest.StartThreadFromMessageAsync(channelId, messageId, BuildFromMessage(), reason, cancellationToken);
    }

    public Task<DiscordChannel> StartFromAsync(IDiscordRest rest, DiscordMessage message, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return StartFromAsync(rest, message.ChannelId, message.Id, reason, cancellationToken);
    }
}
