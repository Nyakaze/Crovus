namespace Crovus.Models;

public sealed record PresenceActivity(string Name, ActivityType Type, string? Url = null, string? State = null)
{
    public static PresenceActivity Playing(string name) => new(name, ActivityType.Playing);

    public static PresenceActivity Listening(string name) => new(name, ActivityType.Listening);

    public static PresenceActivity Watching(string name) => new(name, ActivityType.Watching);

    public static PresenceActivity Competing(string name) => new(name, ActivityType.Competing);

    public static PresenceActivity Streaming(string name, string url) =>
        new(name, ActivityType.Streaming, url);

    public static PresenceActivity Custom(string text) => new("Custom Status", ActivityType.Custom, State: text);
}

public sealed record PresenceUpdate
{
    public UserStatus Status { get; init; } = UserStatus.Online;

    public IReadOnlyList<PresenceActivity> Activities { get; init; } = [];

    public bool Afk { get; init; }

    public DateTimeOffset? IdleSince { get; init; }

    public static PresenceUpdate Online() => new();

    public static PresenceUpdate Idle(DateTimeOffset? since = null) => new()
    {
        Status = UserStatus.Idle,
        Afk = true,
        IdleSince = since ?? DateTimeOffset.UtcNow
    };

    public static PresenceUpdate DoNotDisturb() => new() { Status = UserStatus.DoNotDisturb };

    public static PresenceUpdate Invisible() => new() { Status = UserStatus.Invisible };

    public static PresenceUpdate Playing(string name, UserStatus status = UserStatus.Online) =>
        With(PresenceActivity.Playing(name), status);

    public static PresenceUpdate Listening(string name, UserStatus status = UserStatus.Online) =>
        With(PresenceActivity.Listening(name), status);

    public static PresenceUpdate Watching(string name, UserStatus status = UserStatus.Online) =>
        With(PresenceActivity.Watching(name), status);

    public static PresenceUpdate Competing(string name, UserStatus status = UserStatus.Online) =>
        With(PresenceActivity.Competing(name), status);

    public static PresenceUpdate Streaming(string name, string url, UserStatus status = UserStatus.Online) =>
        With(PresenceActivity.Streaming(name, url), status);

    public static PresenceUpdate Custom(string text, UserStatus status = UserStatus.Online) =>
        With(PresenceActivity.Custom(text), status);

    public static PresenceUpdate With(PresenceActivity activity, UserStatus status = UserStatus.Online)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return new PresenceUpdate { Status = status, Activities = [activity] };
    }
}
