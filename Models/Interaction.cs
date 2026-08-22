using System.Globalization;
using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public enum InteractionType
{
    Ping = 1,
    ApplicationCommand = 2,
    MessageComponent = 3,
    ApplicationCommandAutocomplete = 4,
    ModalSubmit = 5
}

public enum InteractionCallbackType
{
    Pong = 1,
    ChannelMessageWithSource = 4,
    DeferredChannelMessageWithSource = 5,
    DeferredUpdateMessage = 6,
    UpdateMessage = 7,
    ApplicationCommandAutocompleteResult = 8,
    Modal = 9,
    LaunchActivity = 12
}

public enum MessageComponentType
{
    ActionRow = 1,
    Button = 2,
    StringSelect = 3,
    TextInput = 4,
    UserSelect = 5,
    RoleSelect = 6,
    MentionableSelect = 7,
    ChannelSelect = 8
}

[Flags]
public enum MessageFlags
{
    None = 0,
    Crossposted = 1 << 0,
    IsCrosspost = 1 << 1,
    SuppressEmbeds = 1 << 2,
    SourceMessageDeleted = 1 << 3,
    Urgent = 1 << 4,
    HasThread = 1 << 5,
    Ephemeral = 1 << 6,
    Loading = 1 << 7,
    SuppressNotifications = 1 << 12
}

public sealed record DiscordModalField(string CustomId, MessageComponentType Type, string Value);

public sealed record DiscordInteractionOption(string Name, ApplicationCommandOptionType Type, object? Value,
    IReadOnlyList<DiscordInteractionOption> Options, bool Focused)
{
    [JsonIgnore]
    public bool IsSubCommand =>
        Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup;

    [JsonIgnore]
    public bool HasValue => Value is not null;

    public string AsString => Value as string ?? Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty;

    public long AsInteger => Value switch
    {
        Snowflake snowflake => (long)snowflake.Value,
        string text => long.Parse(text, CultureInfo.InvariantCulture),
        _ => Convert.ToInt64(Value, CultureInfo.InvariantCulture)
    };

    public double AsNumber => Value switch
    {
        Snowflake snowflake => snowflake.Value,
        string text => double.Parse(text, CultureInfo.InvariantCulture),
        _ => Convert.ToDouble(Value, CultureInfo.InvariantCulture)
    };

    public bool AsBoolean => Value switch
    {
        bool flag => flag,
        string text => bool.Parse(text),
        _ => Convert.ToBoolean(Value, CultureInfo.InvariantCulture)
    };

    public Snowflake AsSnowflake => Value switch
    {
        Snowflake snowflake => snowflake,
        string text => new Snowflake(ulong.Parse(text, CultureInfo.InvariantCulture)),
        _ => new Snowflake(Convert.ToUInt64(Value, CultureInfo.InvariantCulture))
    };

    public DiscordInteractionOption? Find(string name) =>
        Options.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.Ordinal));
}

public sealed record DiscordInteractionData
{
    public Snowflake? Id { get; init; }

    public string? Name { get; init; }

    public ApplicationCommandType? Type { get; init; }

    public IReadOnlyList<DiscordInteractionOption> Options { get; init; } = [];

    public string? CustomId { get; init; }

    public MessageComponentType? ComponentType { get; init; }

    public IReadOnlyList<string> Values { get; init; } = [];

    public IReadOnlyList<DiscordModalField> Fields { get; init; } = [];

    public Snowflake? TargetId { get; init; }
}

public sealed record DiscordInteraction : IBoundEntity
{
    public static readonly TimeSpan InitialResponseWindow = TimeSpan.FromSeconds(3);

    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public required Snowflake Id { get; init; }

    public required Snowflake ApplicationId { get; init; }

    public required InteractionType Type { get; init; }

    public required string Token { get; init; }

    public int Version { get; init; } = 1;

    public DiscordInteractionData? Data { get; init; }

    public Snowflake? GuildId { get; init; }

    public Snowflake? ChannelId { get; init; }

    public DiscordMember? Member { get; init; }

    public DiscordUser? User { get; init; }

    public DiscordMessage? Message { get; init; }

    public DiscordPermissions ApplicationPermissions { get; init; }

    public string? Locale { get; init; }

    public string? GuildLocale { get; init; }

    [JsonIgnore]
    public DiscordUser? Invoker => Member?.User ?? User;

    [JsonIgnore]
    public bool IsFromGuild => GuildId is not null;

    [JsonIgnore]
    public string CommandName => Data?.Name ?? string.Empty;

    [JsonIgnore]
    public string CustomId => Data?.CustomId ?? string.Empty;

    [JsonIgnore]
    public IReadOnlyList<string> SelectedValues => Data?.Values ?? [];

    [JsonIgnore]
    public string? SelectedValue => SelectedValues.Count == 0 ? null : SelectedValues[0];

    [JsonIgnore]
    public IEnumerable<Snowflake> SelectedIds
    {
        get
        {
            foreach (var value in SelectedValues)
                if (ulong.TryParse(value, out var id))
                    yield return new Snowflake(id);
        }
    }

    [JsonIgnore]
    public bool IsComponent => Type is InteractionType.MessageComponent;

    [JsonIgnore]
    public bool IsModalSubmit => Type is InteractionType.ModalSubmit;

    [JsonIgnore]
    public bool IsButton => Data?.ComponentType is MessageComponentType.Button;

    [JsonIgnore]
    public bool IsSelectMenu => Data?.ComponentType is MessageComponentType.StringSelect
        or MessageComponentType.UserSelect or MessageComponentType.RoleSelect
        or MessageComponentType.MentionableSelect or MessageComponentType.ChannelSelect;

    [JsonIgnore]
    public IReadOnlyList<DiscordModalField> Fields => Data?.Fields ?? [];

    [JsonIgnore]
    public DateTimeOffset CreatedAt => Id.CreatedAt;

    [JsonIgnore]
    public DateTimeOffset RespondBy => CreatedAt + InitialResponseWindow;

    [JsonIgnore]
    public DateTimeOffset ExpiresAt => CreatedAt + TokenLifetime;

    [JsonIgnore]
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    [JsonIgnore]
    public string? SubCommandGroup => Data?.Options
        .FirstOrDefault(option => option.Type is ApplicationCommandOptionType.SubCommandGroup)?.Name;

    [JsonIgnore]
    public string? SubCommand
    {
        get
        {
            var options = Data?.Options ?? [];

            while (options.FirstOrDefault(option => option.IsSubCommand) is { } nested)
            {
                if (nested.Type is ApplicationCommandOptionType.SubCommand)
                    return nested.Name;

                options = nested.Options;
            }

            return null;
        }
    }

    [JsonIgnore]
    public string CommandPath =>
        string.Join(' ', new[] { CommandName, SubCommandGroup, SubCommand }.Where(part => !string.IsNullOrEmpty(part)));

    [JsonIgnore]
    public IReadOnlyList<DiscordInteractionOption> Arguments
    {
        get
        {
            var options = Data?.Options ?? [];

            while (options.FirstOrDefault(option => option.IsSubCommand) is { } nested)
                options = nested.Options;

            return options;
        }
    }

    [JsonIgnore]
    public DiscordInteractionOption? FocusedOption => Arguments.FirstOrDefault(option => option.Focused);

    public DiscordInteractionOption? Option(string name) =>
        Arguments.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.Ordinal));

    public bool Has(string name) => Option(name) is { HasValue: true };

    public string? GetString(string name) => Option(name) is { HasValue: true } option ? option.AsString : null;

    public long? GetInteger(string name) => Option(name) is { HasValue: true } option ? option.AsInteger : null;

    public double? GetNumber(string name) => Option(name) is { HasValue: true } option ? option.AsNumber : null;

    public bool? GetBoolean(string name) => Option(name) is { HasValue: true } option ? option.AsBoolean : null;

    public Snowflake? GetSnowflake(string name) => Option(name) is { HasValue: true } option ? option.AsSnowflake : null;

    public string? GetField(string customId) => Fields
        .FirstOrDefault(field => string.Equals(field.CustomId, customId, StringComparison.Ordinal))?.Value;

    public bool Triggered(string customId) => string.Equals(CustomId, customId, StringComparison.Ordinal);

    public bool TriggeredBy(string prefix) => CustomId.StartsWith(prefix, StringComparison.Ordinal);

    private EntityBinding _binding;

    public DiscordInteraction Bind(ICrovusContext context)
    {
        var bound = this with {
            Member = Member?.Bind(context),
            User = User?.Bind(context),
            Message = Message?.Bind(context)
        };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}
