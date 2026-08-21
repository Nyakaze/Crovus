using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crovus.Models;

namespace Crovus.Json;

public static class DiscordJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        options.Converters.Add(new SnowflakeConverter());
        options.Converters.Add(new EmbedTypeConverter());
        options.Converters.Add(new DiscordPermissionsConverter());
        options.Converters.Add(new DiscordUserConverter());
        options.Converters.Add(new DiscordChannelConverter());
        options.Converters.Add(new DiscordAttachmentConverter());
        options.Converters.Add(new DiscordEmbedConverter());
        options.Converters.Add(new MessageComponentConverter());
        options.Converters.Add(new DiscordMessageConverter());
        options.Converters.Add(new DiscordWebhookConverter());
        options.Converters.Add(new DiscordGuildEmojiConverter());
        options.Converters.Add(new ApplicationCommandChoiceConverter());
        options.Converters.Add(new DiscordInteractionConverter());
        options.Converters.Add(new DiscordRoleConverter());
        options.Converters.Add(new DiscordMemberConverter());
        options.Converters.Add(new DiscordBanConverter());
        options.Converters.Add(new DiscordGuildConverter());
        options.Converters.Add(new UserStatusConverter());
        options.Converters.Add(new DiscordActivityConverter());
        options.Converters.Add(new DiscordClientStatusConverter());
        options.Converters.Add(new DiscordPresenceConverter());
        options.Converters.Add(new GatewayBotInfoConverter());
        options.Converters.Add(new DiscordAuditLogConverter());
        options.Converters.Add(new ThreadListingConverter());
        options.Converters.Add(new DiscordCommandPermissionsConverter());
        options.Converters.Add(new DiscordVoiceStateConverter());
        options.Converters.Add(new DiscordInviteConverter());
        options.Converters.Add(new DiscordThreadMemberConverter());
        options.Converters.Add(new DiscordStickerConverter());
        options.Converters.Add(new DiscordAuditLogEntryConverter());
        options.Converters.Add(new AutoModerationActionConverter());
        options.Converters.Add(new AutoModerationTriggerMetadataConverter());
        options.Converters.Add(new DiscordAutoModerationRuleConverter());
        options.Converters.Add(new DiscordScheduledEventConverter());
        options.Converters.Add(new DiscordStageInstanceConverter());
        options.Converters.Add(new DiscordIntegrationConverter());
        options.Converters.Add(new DiscordEntitlementConverter());

        return options;
    }
}

public sealed class SnowflakeConverter : JsonConverter<Snowflake>
{
    public override Snowflake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => new Snowflake(ulong.Parse(reader.GetString()!, CultureInfo.InvariantCulture)),
            JsonTokenType.Number => new Snowflake(reader.GetUInt64()),
            _ => throw new JsonException($"Cannot read a snowflake from {reader.TokenType}.")
        };

    public override void Write(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
}

public sealed class EmbedTypeConverter : JsonConverter<EmbedType>
{
    public override EmbedType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "image" => EmbedType.Image,
            "video" => EmbedType.Video,
            "gifv" => EmbedType.Gifv,
            "article" => EmbedType.Article,
            "link" => EmbedType.Link,
            "poll_result" => EmbedType.PollResult,
            _ => EmbedType.Rich
        };

    public override void Write(Utf8JsonWriter writer, EmbedType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            EmbedType.Image => "image",
            EmbedType.Video => "video",
            EmbedType.Gifv => "gifv",
            EmbedType.Article => "article",
            EmbedType.Link => "link",
            EmbedType.PollResult => "poll_result",
            _ => "rich"
        });
}

public sealed class DiscordPermissionsConverter : JsonConverter<DiscordPermissions>
{
    public override DiscordPermissions Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => Parse(reader.GetString()),
            JsonTokenType.Number => (DiscordPermissions)reader.GetUInt64(),
            JsonTokenType.Null => DiscordPermissions.None,
            _ => throw new JsonException($"Cannot read permissions from {reader.TokenType}.")
        };

    private static DiscordPermissions Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DiscordPermissions.None;

        if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bits))
            return (DiscordPermissions)bits;

        throw new JsonException($"Cannot read permissions from '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, DiscordPermissions value, JsonSerializerOptions options) =>
        writer.WriteStringValue(((ulong)value).ToString(CultureInfo.InvariantCulture));
}
