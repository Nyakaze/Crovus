using Crovus.Models;

namespace Crovus.RichPresence;

public enum RichPresenceStatusDisplay
{
    Name = 0,
    State = 1,
    Details = 2
}

public sealed record RichPresenceAssets(
    string? LargeImage = null,
    string? LargeText = null,
    string? SmallImage = null,
    string? SmallText = null);

public sealed record RichPresenceParty(string? Id = null, int? CurrentSize = null, int? MaxSize = null);

public sealed record RichPresenceButton(string Label, string Url);

public sealed record RichPresenceActivity
{
    public ActivityType Type { get; init; } = ActivityType.Playing;

    public string? Name { get; init; }

    public RichPresenceStatusDisplay? StatusDisplay { get; init; }

    public string? Details { get; init; }

    public string? DetailsUrl { get; init; }

    public string? State { get; init; }

    public string? StateUrl { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }

    public RichPresenceAssets? Assets { get; init; }

    public RichPresenceParty? Party { get; init; }

    public IReadOnlyList<RichPresenceButton> Buttons { get; init; } = [];

    public bool Instance { get; init; }

    public static RichPresenceActivity Playing(string? details = null, string? state = null) =>
        new() { Type = ActivityType.Playing, Details = details, State = state };

    public static RichPresenceActivity Listening(string? details = null, string? state = null) =>
        new() { Type = ActivityType.Listening, Details = details, State = state };

    public static RichPresenceActivity Watching(string? details = null, string? state = null) =>
        new() { Type = ActivityType.Watching, Details = details, State = state };

    public static RichPresenceActivity Competing(string? details = null, string? state = null) =>
        new() { Type = ActivityType.Competing, Details = details, State = state };

    public RichPresenceActivity WithElapsed(DateTimeOffset startedAt) => this with { StartedAt = startedAt, EndsAt = null };

    public RichPresenceActivity WithProgress(TimeSpan position, TimeSpan duration, DateTimeOffset? now = null)
    {
        var reference = now ?? DateTimeOffset.UtcNow;
        var startedAt = reference - position;

        return this with { StartedAt = startedAt, EndsAt = startedAt + duration };
    }

    public RichPresenceActivity WithButton(string label, string url) =>
        this with { Buttons = [.. Buttons, new RichPresenceButton(label, url)] };

    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        if (Type is not (ActivityType.Playing or ActivityType.Listening or ActivityType.Watching or ActivityType.Competing))
            problems.Add($"Activity type {Type} cannot be set over Rich Presence; use Playing, Listening, Watching or Competing.");

        CheckText(problems, "name", Name);
        CheckText(problems, "details", Details);
        CheckText(problems, "state", State);
        CheckUrl(problems, "details URL", DetailsUrl);
        CheckUrl(problems, "state URL", StateUrl);

        if (StartedAt is { } start && EndsAt is { } end && end <= start)
            problems.Add("The end timestamp must be after the start timestamp.");

        if (Assets is { } assets)
        {
            CheckAsset(problems, "large image", assets.LargeImage);
            CheckText(problems, "large image text", assets.LargeText);
            CheckAsset(problems, "small image", assets.SmallImage);
            CheckText(problems, "small image text", assets.SmallText);
        }

        if (Party is { } party)
        {
            if (party.Id is { Length: > RichPresenceLimits.PartyId })
                problems.Add($"The party id is longer than {RichPresenceLimits.PartyId} characters.");

            if (party.CurrentSize is { } current && party.MaxSize is { } max && (current < 1 || max < current))
                problems.Add("The party size must be at least 1 and no larger than the maximum size.");

            if (party.CurrentSize is null != party.MaxSize is null)
                problems.Add("The party size needs both the current and the maximum size.");
        }

        if (Buttons.Count > RichPresenceLimits.Buttons)
            problems.Add($"At most {RichPresenceLimits.Buttons} buttons are allowed.");

        foreach (var button in Buttons)
        {
            if (string.IsNullOrWhiteSpace(button.Label) || button.Label.Length > RichPresenceLimits.ButtonLabel)
                problems.Add($"Button label '{button.Label}' must be 1 to {RichPresenceLimits.ButtonLabel} characters.");

            CheckUrl(problems, $"button '{button.Label}' URL", button.Url, required: true);
        }

        return problems;
    }

    public void EnsureValid()
    {
        var problems = Validate();

        if (problems.Count > 0)
            throw new ArgumentException("The Rich Presence activity is invalid: " + string.Join(" ", problems));
    }

    private static void CheckText(List<string> problems, string field, string? value)
    {
        if (value is null)
            return;

        if (value.Length < RichPresenceLimits.TextMin || value.Length > RichPresenceLimits.TextMax)
            problems.Add($"The {field} must be {RichPresenceLimits.TextMin} to {RichPresenceLimits.TextMax} characters, but is {value.Length}.");
    }

    private static void CheckAsset(List<string> problems, string field, string? value)
    {
        if (value is { Length: > RichPresenceLimits.AssetKey })
            problems.Add($"The {field} is longer than {RichPresenceLimits.AssetKey} characters.");
    }

    private static void CheckUrl(List<string> problems, string field, string? value, bool required = false)
    {
        if (value is null)
        {
            if (required)
                problems.Add($"The {field} is missing.");

            return;
        }

        if (value.Length > RichPresenceLimits.Url)
            problems.Add($"The {field} is longer than {RichPresenceLimits.Url} characters.");
        else if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            problems.Add($"The {field} must be an absolute http or https URL.");
    }
}

public static class RichPresenceLimits
{
    public const int TextMin = 2;
    public const int TextMax = 128;
    public const int AssetKey = 256;
    public const int PartyId = 128;
    public const int Buttons = 2;
    public const int ButtonLabel = 32;
    public const int Url = 512;
    public const int UpdatesPerWindow = 5;

    public static readonly TimeSpan UpdateWindow = TimeSpan.FromSeconds(20);
}
