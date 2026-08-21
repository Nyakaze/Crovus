namespace Crovus.Cache;

public sealed record CacheOptions
{
    public CachePolicy Channels { get; init; } = new(1_000);

    public CachePolicy Messages { get; init; } = new(1_000, TimeSpan.FromMinutes(30));

    public CachePolicy Users { get; init; } = new(2_000, TimeSpan.FromHours(1));

    public CachePolicy Webhooks { get; init; } = new(500, TimeSpan.FromMinutes(30));

    public CachePolicy ChannelWebhooks { get; init; } = new(500, TimeSpan.FromMinutes(10));

    public CachePolicy Reactions { get; init; } = new(2_000, TimeSpan.FromMinutes(30));

    public CachePolicy Guilds { get; init; } = new(200);

    public CachePolicy Members { get; init; } = new(5_000, TimeSpan.FromMinutes(30));

    public CachePolicy GuildRoles { get; init; } = new(200, TimeSpan.FromMinutes(30));

    public static CacheOptions Disabled { get; } = new()
    {
        Channels = CachePolicy.Disabled,
        Messages = CachePolicy.Disabled,
        Users = CachePolicy.Disabled,
        Webhooks = CachePolicy.Disabled,
        ChannelWebhooks = CachePolicy.Disabled,
        Reactions = CachePolicy.Disabled,
        Guilds = CachePolicy.Disabled,
        Members = CachePolicy.Disabled,
        GuildRoles = CachePolicy.Disabled
    };
}
