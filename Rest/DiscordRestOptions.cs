namespace Crovus.Rest;

public sealed record DiscordRestOptions
{
    public required string Token { get; init; }

    public string TokenType { get; init; } = "Bot";

    public string BaseUrl { get; init; } = "https://discord.com/api";

    public int ApiVersion { get; init; } = 10;

    public string UserAgent { get; init; } = "DiscordBot (https://github.com/Nyakaze/Crovus, 1.0)";

    public int MaxAttempts { get; init; } = 3;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public Uri BuildBaseAddress() => new($"{BaseUrl.TrimEnd('/')}/v{ApiVersion}/");

    internal string BuildAuthorization() => $"{TokenType} {Token}";
}
