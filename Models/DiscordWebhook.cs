using Crovus.Client;

namespace Crovus.Models;

public sealed record DiscordWebhook(Snowflake Id, WebhookType Type, Snowflake ChannelId, Snowflake? GuildId,
    string? Name, string? Avatar, string? Token, Snowflake? ApplicationId, DiscordUser? Creator) : IBoundEntity
{
    public bool CanExecute => Type is WebhookType.Incoming && Token is not null;

    public string? Url => Token is null ? null : $"https://discord.com/api/v10/webhooks/{Id.Value}/{Token}";

    public string AvatarUrl => Avatar is null
        ? "https://cdn.discordapp.com/embed/avatars/0.png"
        : $"https://cdn.discordapp.com/avatars/{Id.Value}/{Avatar}.{(Avatar.StartsWith("a_") ? "gif" : "png")}";

    public string ExecuteUrl(Snowflake? threadId = null, bool wait = false)
    {
        if (Url is not { } url)
            throw new InvalidOperationException($"Webhook {Id} has no token and cannot be executed.");

        var query = new List<string>(2);
        if (wait) query.Add("wait=true");
        if (threadId is { } thread) query.Add($"thread_id={thread.Value}");

        return query.Count == 0 ? url : $"{url}?{string.Join('&', query)}";
    }

    public bool Targets(DiscordChannel channel) => ChannelId == channel.WebhookChannelId;

    private EntityBinding _binding;

    public DiscordWebhook Bind(ICrovusContext context)
    {
        var bound = this with { Creator = Creator?.Bind(context) };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}
