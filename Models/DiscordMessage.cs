using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public sealed record DiscordMessage(Snowflake Id, Snowflake ChannelId, Snowflake? GuildId,
    DiscordUser Author, string Content, bool IsWebhook,
    IReadOnlyList<DiscordAttachment> Attachments,
    IReadOnlyList<DiscordEmbed> Embeds,
    DiscordMessageReference? ReferencedMessage) : IBoundEntity
{
    [JsonIgnore]
    public bool IsPartial { get; init; }

    public static DiscordMessage Partial(Snowflake id, Snowflake channelId, Snowflake? guildId = null) =>
        new(id, channelId, guildId, DiscordUser.Partial(default), string.Empty, false, [], [], null)
        {
            IsPartial = true
        };

    public DiscordMessage In(Snowflake guildId) => GuildId is null ? this with { GuildId = guildId } : this;

    public IReadOnlyList<DiscordComponent> Components { get; init; } = [];

    public bool HasComponents => Components.Count > 0;

    public IEnumerable<DiscordComponent> AllComponents =>
        Components.SelectMany(component => component.Flatten());

    public DiscordComponent? Component(string customId) =>
        AllComponents.FirstOrDefault(component =>
            string.Equals(component.CustomId, customId, StringComparison.Ordinal));

    public IEnumerable<DiscordButton> Buttons => AllComponents.OfType<DiscordButton>();

    public IEnumerable<DiscordSelectMenu> SelectMenus => AllComponents.OfType<DiscordSelectMenu>();

    private EntityBinding _binding;

    public DiscordMessage Bind(ICrovusContext context)
    {
        var bound = this with { Author = Author.Bind(context) };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}
