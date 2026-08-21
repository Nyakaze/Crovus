namespace Crovus.Models;

public sealed record DiscordAttachment(Snowflake Id, string FileName, string Url, string ProxyUrl, int Size,
    string? ContentType, int? Width, int? Height, string? Description, bool Ephemeral)
{
    public bool IsImage => Width is not null && Height is not null;
}
