namespace Crovus.Models;

public sealed record WebhookCreateRequest(string Name, string? AvatarData = null);

public sealed record WebhookModifyRequest(string? Name = null, string? AvatarData = null,
    Snowflake? ChannelId = null);

public sealed record WebhookExecuteRequest(string? Content = null, IReadOnlyList<DiscordEmbed>? Embeds = null,
    string? Username = null, string? AvatarUrl = null, string? ThreadName = null, bool Tts = false,
    IReadOnlyList<DiscordFile>? Files = null, IReadOnlyList<DiscordComponent>? Components = null)
{
    public bool HasFiles => Files is { Count: > 0 };

    public bool HasComponents => Components is { Count: > 0 };

    public WebhookExecuteRequest Attaching(DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return this with { Files = [.. Files ?? [], file] };
    }
}
