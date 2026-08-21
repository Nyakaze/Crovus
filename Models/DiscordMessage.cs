namespace Crovus.Models;

public sealed record DiscordMessage(Snowflake Id, Snowflake ChannelId, Snowflake? GuildId,
    DiscordUser Author, string Content, bool IsWebhook,
    IReadOnlyList<DiscordAttachment> Attachments,
    IReadOnlyList<DiscordEmbed> Embeds,
    DiscordMessageReference? ReferencedMessage)
{
    public IReadOnlyList<DiscordComponent> Components { get; init; } = [];

    public bool HasComponents => Components.Count > 0;

    public IEnumerable<DiscordComponent> AllComponents =>
        Components.SelectMany(component => component.Flatten());

    public DiscordComponent? Component(string customId) =>
        AllComponents.FirstOrDefault(component =>
            string.Equals(component.CustomId, customId, StringComparison.Ordinal));

    public IEnumerable<DiscordButton> Buttons => AllComponents.OfType<DiscordButton>();

    public IEnumerable<DiscordSelectMenu> SelectMenus => AllComponents.OfType<DiscordSelectMenu>();
}
