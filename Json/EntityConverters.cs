using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crovus.Models;

namespace Crovus.Json;

public sealed class DiscordUserConverter : JsonConverter<DiscordUser>
{
    public override DiscordUser Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordUser(
            element.RequireSnowflake("id"),
            element.StringOrNull("username") ?? string.Empty,
            element.StringOrNull("global_name"),
            element.StringOrNull("discriminator") is { } discriminator && discriminator != "0"
                ? discriminator
                : null,
            element.StringOrNull("avatar"),
            element.Flag("bot"));
    }

    public override void Write(Utf8JsonWriter writer, DiscordUser value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord users are read-only.");
}

public sealed class DiscordChannelConverter : JsonConverter<DiscordChannel>
{
    public override DiscordChannel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var type = (ChannelType)element.RequireInt32("type");

        return new DiscordChannel(
            element.RequireSnowflake("id"),
            element.SnowflakeOrNull("guild_id"),
            type,
            element.StringOrNull("name") ?? string.Empty,
            element.SnowflakeOrNull("parent_id"),
            type is ChannelType.AnnouncementThread or ChannelType.PublicThread or ChannelType.PrivateThread);
    }

    public override void Write(Utf8JsonWriter writer, DiscordChannel value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord channels are read-only.");
}

public sealed class DiscordAttachmentConverter : JsonConverter<DiscordAttachment>
{
    public override DiscordAttachment Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordAttachment(
            element.RequireSnowflake("id"),
            element.RequireString("filename"),
            element.RequireString("url"),
            element.StringOrNull("proxy_url") ?? string.Empty,
            element.Int32OrNull("size") ?? 0,
            element.StringOrNull("content_type"),
            element.Int32OrNull("width"),
            element.Int32OrNull("height"),
            element.StringOrNull("description"),
            element.Flag("ephemeral"));
    }

    public override void Write(Utf8JsonWriter writer, DiscordAttachment value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord attachments are read-only.");
}

public sealed class DiscordEmbedConverter : JsonConverter<DiscordEmbed>
{
    public override DiscordEmbed Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordEmbed(
            element.StringOrNull("title"),
            element.StringOrNull("description"),
            element.StringOrNull("url"),
            element.Deserialize<EmbedType>("type", options),
            element.Property("timestamp")?.GetDateTimeOffset(),
            element.Int32OrNull("color"),
            element.Deserialize<DiscordEmbedAuthor>("author", options),
            element.Deserialize<DiscordEmbedFooter>("footer", options),
            element.Deserialize<DiscordEmbedMedia>("image", options),
            element.Deserialize<DiscordEmbedMedia>("thumbnail", options),
            element.Deserialize<DiscordEmbedMedia>("video", options),
            element.Deserialize<DiscordEmbedProvider>("provider", options),
            element.DeserializeList<DiscordEmbedField>("fields", options));
    }

    public override void Write(Utf8JsonWriter writer, DiscordEmbed value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        WriteOptionalString(writer, "title", value.Title);
        WriteOptionalString(writer, "description", value.Description);
        WriteOptionalString(writer, "url", value.Url);

        if (value.Timestamp is { } timestamp)
            writer.WriteString("timestamp", timestamp);

        if (value.Color is { } color)
            writer.WriteNumber("color", color);

        WriteOptional(writer, "author", value.Author, options);
        WriteOptional(writer, "footer", value.Footer, options);
        WriteOptional(writer, "image", value.Image, options);
        WriteOptional(writer, "thumbnail", value.Thumbnail, options);

        if (value.Fields is { Count: > 0 })
        {
            writer.WritePropertyName("fields");
            JsonSerializer.Serialize(writer, value.Fields, options);
        }

        writer.WriteEndObject();
    }

    private static void WriteOptionalString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
            writer.WriteString(name, value);
    }

    private static void WriteOptional<T>(Utf8JsonWriter writer, string name, T? value, JsonSerializerOptions options)
        where T : class
    {
        if (value is null)
            return;

        writer.WritePropertyName(name);
        JsonSerializer.Serialize(writer, value, options);
    }
}

public sealed class DiscordMessageConverter : JsonConverter<DiscordMessage>
{
    public override DiscordMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordMessage(
            element.RequireSnowflake("id"),
            element.RequireSnowflake("channel_id"),
            element.SnowflakeOrNull("guild_id"),
            element.Deserialize<DiscordUser>("author", options) ??
            new DiscordUser(default, string.Empty, null, null, null, false),
            element.StringOrNull("content") ?? string.Empty,
            element.Property("webhook_id") is not null,
            element.DeserializeList<DiscordAttachment>("attachments", options),
            element.DeserializeList<DiscordEmbed>("embeds", options),
            element.Deserialize<DiscordMessageReference>("message_reference", options))
        {
            Components = MessageComponentConverter.ReadList(element.Property("components"), options)
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordMessage value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord messages are read-only.");
}

public sealed class DiscordWebhookConverter : JsonConverter<DiscordWebhook>
{
    public override DiscordWebhook Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordWebhook(
            element.RequireSnowflake("id"),
            (WebhookType)(element.Int32OrNull("type") ?? (int)WebhookType.Incoming),
            element.SnowflakeOrNull("channel_id") ?? default,
            element.SnowflakeOrNull("guild_id"),
            element.StringOrNull("name"),
            element.StringOrNull("avatar"),
            element.StringOrNull("token"),
            element.SnowflakeOrNull("application_id"),
            element.Deserialize<DiscordUser>("user", options));
    }

    public override void Write(Utf8JsonWriter writer, DiscordWebhook value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord webhooks are read-only.");
}

public sealed class DiscordGuildEmojiConverter : JsonConverter<DiscordGuildEmoji>
{
    public override DiscordGuildEmoji Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordGuildEmoji(
            element.RequireSnowflake("id"),
            element.StringOrNull("name") ?? string.Empty,
            element.DeserializeList<Snowflake>("roles", options),
            element.Deserialize<DiscordUser>("user", options),
            element.Flag("animated"),
            element.Flag("managed"),
            element.Property("available") is not { ValueKind: JsonValueKind.False },
            element.Property("require_colons") is not { ValueKind: JsonValueKind.False });
    }

    public override void Write(Utf8JsonWriter writer, DiscordGuildEmoji value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Guild emojis are read-only.");
}

public sealed class ApplicationCommandChoiceConverter : JsonConverter<DiscordApplicationCommandChoice>
{
    public override DiscordApplicationCommandChoice Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        if (element.Property("value") is not { } value)
            throw new JsonException("A command choice needs a value.");

        object parsed = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()!,
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => value.GetDouble(),
            _ => throw new JsonException($"A command choice cannot hold a {value.ValueKind} value.")
        };

        return new DiscordApplicationCommandChoice(element.RequireString("name"), parsed);
    }

    public override void Write(Utf8JsonWriter writer, DiscordApplicationCommandChoice value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WritePropertyName("value");

        switch (value.Value)
        {
            case string text:
                writer.WriteStringValue(text);
                break;
            case double or float or decimal:
                writer.WriteNumberValue(Convert.ToDouble(value.Value, CultureInfo.InvariantCulture));
                break;
            case byte or short or int or long:
                writer.WriteNumberValue(Convert.ToInt64(value.Value, CultureInfo.InvariantCulture));
                break;
            default:
                throw new JsonException($"A command choice cannot hold a {value.Value.GetType().Name} value.");
        }

        writer.WriteEndObject();
    }
}

public sealed class DiscordInteractionConverter : JsonConverter<DiscordInteraction>
{
    public override DiscordInteraction Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordInteraction
        {
            Id = element.RequireSnowflake("id"),
            ApplicationId = element.RequireSnowflake("application_id"),
            Type = (InteractionType)element.RequireInt32("type"),
            Token = element.RequireString("token"),
            Version = element.Int32OrNull("version") ?? 1,
            Data = ReadData(element.Property("data")),
            GuildId = element.SnowflakeOrNull("guild_id"),
            ChannelId = element.SnowflakeOrNull("channel_id"),
            Member = ReadMember(element.Property("member"), element.SnowflakeOrNull("guild_id"), options),
            User = element.Deserialize<DiscordUser>("user", options),
            Message = element.Deserialize<DiscordMessage>("message", options),
            ApplicationPermissions = element.Deserialize<DiscordPermissions>("app_permissions", options),
            Locale = element.StringOrNull("locale"),
            GuildLocale = element.StringOrNull("guild_locale")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordInteraction value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord interactions are read-only.");

    private static DiscordMember? ReadMember(JsonElement? element, Snowflake? guildId,
        JsonSerializerOptions options)
    {
        if (element is not { } member || member.Property("user") is null)
            return null;

        var parsed = DiscordMemberConverter.Read(member, options);

        return parsed.GuildId is null && guildId is { } guild ? parsed.In(guild) : parsed;
    }

    private static DiscordInteractionData? ReadData(JsonElement? element)
    {
        if (element is not { } data)
            return null;

        return new DiscordInteractionData
        {
            Id = data.SnowflakeOrNull("id"),
            Name = data.StringOrNull("name"),
            Type = data.Int32OrNull("type") is { } type && data.Property("component_type") is null
                ? (ApplicationCommandType)type
                : null,
            Options = ReadOptions(data.Property("options")),
            CustomId = data.StringOrNull("custom_id"),
            ComponentType = data.Int32OrNull("component_type") is { } component
                ? (MessageComponentType)component
                : null,
            Values = ReadValues(data.Property("values")),
            Fields = ReadFields(data.Property("components")),
            TargetId = data.SnowflakeOrNull("target_id")
        };
    }

    private static IReadOnlyList<DiscordInteractionOption> ReadOptions(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var options = new List<DiscordInteractionOption>(array.GetArrayLength());

        foreach (var option in array.EnumerateArray())
            options.Add(ReadOption(option));

        return options;
    }

    private static DiscordInteractionOption ReadOption(JsonElement element)
    {
        var type = (ApplicationCommandOptionType)element.RequireInt32("type");

        return new DiscordInteractionOption(
            element.RequireString("name"),
            type,
            element.Property("value") is { } value ? ReadValue(value, type) : null,
            ReadOptions(element.Property("options")),
            element.Flag("focused"));
    }

    private static object? ReadValue(JsonElement value, ApplicationCommandOptionType type) => value.ValueKind switch
    {
        JsonValueKind.String when IsSnowflake(type) =>
            new Snowflake(ulong.Parse(value.GetString()!, CultureInfo.InvariantCulture)),
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static bool IsSnowflake(ApplicationCommandOptionType type) =>
        type is ApplicationCommandOptionType.User or ApplicationCommandOptionType.Channel
            or ApplicationCommandOptionType.Role or ApplicationCommandOptionType.Mentionable
            or ApplicationCommandOptionType.Attachment;

    private static IReadOnlyList<string> ReadValues(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var values = new List<string>(array.GetArrayLength());

        foreach (var value in array.EnumerateArray())
        {
            if (value.GetString() is { } text)
                values.Add(text);
        }

        return values;
    }

    private static IReadOnlyList<DiscordModalField> ReadFields(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } rows)
            return [];

        var fields = new List<DiscordModalField>();

        foreach (var row in rows.EnumerateArray())
        {
            if (row.Property("components") is not { ValueKind: JsonValueKind.Array } children)
                continue;

            foreach (var child in children.EnumerateArray())
            {
                if (child.StringOrNull("custom_id") is not { } customId)
                    continue;

                fields.Add(new DiscordModalField(customId,
                    (MessageComponentType)(child.Int32OrNull("type") ?? (int)MessageComponentType.TextInput),
                    child.StringOrNull("value") ?? string.Empty));
            }
        }

        return fields;
    }
}

public sealed class DiscordRoleConverter : JsonConverter<DiscordRole>
{
    public override DiscordRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordRole(
            element.RequireSnowflake("id"),
            element.SnowflakeOrNull("guild_id"),
            element.StringOrNull("name") ?? string.Empty,
            element.Int32OrNull("color") ?? 0,
            element.Flag("hoist"),
            element.Int32OrNull("position") ?? 0,
            element.Deserialize<DiscordPermissions>("permissions", options),
            element.Flag("managed"),
            element.Flag("mentionable"),
            element.StringOrNull("icon"),
            element.StringOrNull("unicode_emoji"),
            ReadTags(element.Property("tags")));
    }

    public override void Write(Utf8JsonWriter writer, DiscordRole value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord roles are read-only.");

    private static DiscordRoleTags? ReadTags(JsonElement? element)
    {
        if (element is not { } tags)
            return null;

        return new DiscordRoleTags(
            tags.SnowflakeOrNull("bot_id"),
            tags.SnowflakeOrNull("integration_id"),
            tags.TryGetProperty("premium_subscriber", out _),
            tags.SnowflakeOrNull("subscription_listing_id"),
            tags.TryGetProperty("available_for_purchase", out _),
            tags.TryGetProperty("guild_connections", out _));
    }
}

public sealed class DiscordMemberConverter : JsonConverter<DiscordMember>
{
    public override DiscordMember Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return Read(element, options);
    }

    public override void Write(Utf8JsonWriter writer, DiscordMember value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord members are read-only.");

    internal static DiscordMember Read(JsonElement element, JsonSerializerOptions options,
        DiscordUser? fallbackUser = null) => new()
    {
        User = element.Deserialize<DiscordUser>("user", options) ?? fallbackUser ??
               throw new JsonException("Expected a guild member to carry a user."),
        GuildId = element.SnowflakeOrNull("guild_id"),
        Nickname = element.StringOrNull("nick"),
        Avatar = element.StringOrNull("avatar"),
        Roles = element.SnowflakeList("roles"),
        JoinedAt = element.TimestampOrNull("joined_at"),
        PremiumSince = element.TimestampOrNull("premium_since"),
        CommunicationDisabledUntil = element.TimestampOrNull("communication_disabled_until"),
        Permissions = element.Deserialize<DiscordPermissions>("permissions", options),
        Flags = (GuildMemberFlags)(element.Int32OrNull("flags") ?? 0),
        Deaf = element.Flag("deaf"),
        Mute = element.Flag("mute"),
        Pending = element.Flag("pending")
    };
}

public sealed class DiscordBanConverter : JsonConverter<DiscordBan>
{
    public override DiscordBan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordBan(
            element.Deserialize<DiscordUser>("user", options) ??
            throw new JsonException("Expected a ban to carry a user."),
            element.StringOrNull("reason"));
    }

    public override void Write(Utf8JsonWriter writer, DiscordBan value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord bans are read-only.");
}

public sealed class DiscordGuildConverter : JsonConverter<DiscordGuild>
{
    public override DiscordGuild Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var id = element.RequireSnowflake("id");

        return new DiscordGuild
        {
            Id = id,
            Name = element.StringOrNull("name") ?? string.Empty,
            OwnerId = element.SnowflakeOrNull("owner_id") ?? default,
            Icon = element.StringOrNull("icon"),
            Banner = element.StringOrNull("banner"),
            Splash = element.StringOrNull("splash"),
            Description = element.StringOrNull("description"),
            VanityUrlCode = element.StringOrNull("vanity_url_code"),
            PreferredLocale = element.StringOrNull("preferred_locale") ?? "en-US",
            AfkChannelId = element.SnowflakeOrNull("afk_channel_id"),
            AfkTimeout = element.Int32OrNull("afk_timeout") ?? 0,
            SystemChannelId = element.SnowflakeOrNull("system_channel_id"),
            RulesChannelId = element.SnowflakeOrNull("rules_channel_id"),
            PublicUpdatesChannelId = element.SnowflakeOrNull("public_updates_channel_id"),
            VerificationLevel = (VerificationLevel)(element.Int32OrNull("verification_level") ?? 0),
            DefaultMessageNotifications =
                (MessageNotificationLevel)(element.Int32OrNull("default_message_notifications") ?? 0),
            ExplicitContentFilter = (ExplicitContentFilterLevel)(element.Int32OrNull("explicit_content_filter") ?? 0),
            MfaLevel = (MfaLevel)(element.Int32OrNull("mfa_level") ?? 0),
            NsfwLevel = (GuildNsfwLevel)(element.Int32OrNull("nsfw_level") ?? 0),
            PremiumTier = (PremiumTier)(element.Int32OrNull("premium_tier") ?? 0),
            PremiumSubscriptionCount = element.Int32OrNull("premium_subscription_count") ?? 0,
            MemberCount = element.Int32OrNull("member_count"),
            MaxMembers = element.Int32OrNull("max_members"),
            ApproximateMemberCount = element.Int32OrNull("approximate_member_count"),
            ApproximatePresenceCount = element.Int32OrNull("approximate_presence_count"),
            Large = element.Flag("large"),
            Unavailable = element.Flag("unavailable"),
            Features = element.StringList("features"),
            Roles = ReadRoles(element, id, options),
            Emojis = element.DeserializeList<DiscordGuildEmoji>("emojis", options)
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordGuild value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord guilds are read-only.");

    private static IReadOnlyList<DiscordRole> ReadRoles(JsonElement element, Snowflake guildId,
        JsonSerializerOptions options)
    {
        var roles = element.DeserializeList<DiscordRole>("roles", options);

        return roles.Count == 0
            ? roles
            : roles.Select(role => role.GuildId is null ? role with { GuildId = guildId } : role).ToArray();
    }
}

public sealed class UserStatusConverter : JsonConverter<UserStatus>
{
    public override UserStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, UserStatus value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Format(value));

    public static UserStatus Parse(string? raw) => raw switch
    {
        "online" => UserStatus.Online,
        "idle" => UserStatus.Idle,
        "dnd" => UserStatus.DoNotDisturb,
        "invisible" => UserStatus.Invisible,
        _ => UserStatus.Offline
    };

    public static string Format(UserStatus status) => status switch
    {
        UserStatus.Online => "online",
        UserStatus.Idle => "idle",
        UserStatus.DoNotDisturb => "dnd",
        UserStatus.Invisible => "invisible",
        _ => "offline"
    };
}

public sealed class DiscordActivityConverter : JsonConverter<DiscordActivity>
{
    public override DiscordActivity Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordActivity
        {
            Name = element.StringOrNull("name") ?? string.Empty,
            Type = (ActivityType)(element.Int32OrNull("type") ?? 0),
            Url = element.StringOrNull("url"),
            CreatedAt = ReadUnixMillis(element, "created_at"),
            Timestamps = ReadTimestamps(element),
            ApplicationId = element.SnowflakeOrNull("application_id"),
            Details = element.StringOrNull("details"),
            State = element.StringOrNull("state"),
            Emoji = element.Deserialize<DiscordEmoji>("emoji", options),
            Party = ReadParty(element),
            Assets = ReadAssets(element),
            Flags = (ActivityFlags)(element.Int32OrNull("flags") ?? 0),
            Buttons = ReadButtons(element)
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordActivity value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord activities are read-only.");

    private static DateTimeOffset? ReadUnixMillis(JsonElement element, string name)
    {
        if (element.Property(name) is not { } value)
            return null;

        if (value.ValueKind is JsonValueKind.Number && value.TryGetInt64(out var millis))
            return DateTimeOffset.FromUnixTimeMilliseconds(millis);

        if (value.ValueKind is JsonValueKind.String && long.TryParse(value.GetString(), out var parsed))
            return DateTimeOffset.FromUnixTimeMilliseconds(parsed);

        return null;
    }

    private static DiscordActivityTimestamps? ReadTimestamps(JsonElement element)
    {
        if (element.Property("timestamps") is not { ValueKind: JsonValueKind.Object } timestamps)
            return null;

        var start = ReadUnixMillis(timestamps, "start");
        var end = ReadUnixMillis(timestamps, "end");

        return start is null && end is null ? null : new DiscordActivityTimestamps(start, end);
    }

    private static DiscordActivityParty? ReadParty(JsonElement element)
    {
        if (element.Property("party") is not { ValueKind: JsonValueKind.Object } party)
            return null;

        int? current = null;
        int? max = null;

        if (party.Property("size") is { ValueKind: JsonValueKind.Array } size && size.GetArrayLength() >= 2)
        {
            current = size[0].GetInt32();
            max = size[1].GetInt32();
        }

        return new DiscordActivityParty(party.StringOrNull("id"), current, max);
    }

    private static DiscordActivityAssets? ReadAssets(JsonElement element)
    {
        if (element.Property("assets") is not { ValueKind: JsonValueKind.Object } assets)
            return null;

        return new DiscordActivityAssets(
            assets.StringOrNull("large_image"),
            assets.StringOrNull("large_text"),
            assets.StringOrNull("small_image"),
            assets.StringOrNull("small_text"));
    }

    private static IReadOnlyList<string> ReadButtons(JsonElement element)
    {
        if (element.Property("buttons") is not { ValueKind: JsonValueKind.Array } buttons)
            return [];

        var labels = new List<string>(buttons.GetArrayLength());

        foreach (var button in buttons.EnumerateArray())
        {
            if (button.ValueKind is JsonValueKind.String && button.GetString() is { } label)
                labels.Add(label);
            else if (button.ValueKind is JsonValueKind.Object && button.StringOrNull("label") is { } named)
                labels.Add(named);
        }

        return labels;
    }
}

public sealed class DiscordClientStatusConverter : JsonConverter<DiscordClientStatus>
{
    public override DiscordClientStatus Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordClientStatus(
            ReadStatus(element, "desktop"),
            ReadStatus(element, "mobile"),
            ReadStatus(element, "web"));
    }

    public override void Write(Utf8JsonWriter writer, DiscordClientStatus value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Client statuses are read-only.");

    private static UserStatus? ReadStatus(JsonElement element, string name) =>
        element.StringOrNull(name) is { } raw ? UserStatusConverter.Parse(raw) : null;
}

public sealed class DiscordPresenceConverter : JsonConverter<DiscordPresence>
{
    public override DiscordPresence Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var user = element.Deserialize<DiscordUser>("user", options);

        var userId = user?.Id ??
                     element.Property("user")?.SnowflakeOrNull("id") ??
                     throw new JsonException("Expected a presence to carry a user id.");

        return new DiscordPresence
        {
            UserId = userId,
            GuildId = element.SnowflakeOrNull("guild_id"),
            User = user is { Username.Length: > 0 } ? user : null,
            Status = UserStatusConverter.Parse(element.StringOrNull("status")),
            Activities = element.DeserializeList<DiscordActivity>("activities", options),
            ClientStatus = element.Deserialize<DiscordClientStatus>("client_status", options) ??
                           DiscordClientStatus.None
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordPresence value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord presences are read-only.");
}

public sealed class GatewayBotInfoConverter : JsonConverter<GatewayBotInfo>
{
    public override GatewayBotInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var limit = element.Property("session_start_limit");

        return new GatewayBotInfo
        {
            Url = element.StringOrNull("url") ?? throw new JsonException("Expected a gateway url."),
            Shards = element.IntegerOrNull("shards") ?? 1,
            SessionStartLimit = limit is { } value
                ? new SessionStartLimit(
                    value.IntegerOrNull("total") ?? 0,
                    value.IntegerOrNull("remaining") ?? 0,
                    TimeSpan.FromMilliseconds(value.Property("reset_after")?.GetInt64() ?? 0),
                    value.IntegerOrNull("max_concurrency") ?? 1)
                : new SessionStartLimit(1000, 1000, TimeSpan.Zero, 1)
        };
    }

    public override void Write(Utf8JsonWriter writer, GatewayBotInfo value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Gateway information is read-only.");
}

public sealed class DiscordAuditLogConverter : JsonConverter<DiscordAuditLog>
{
    public override DiscordAuditLog Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordAuditLog
        {
            Entries = element.DeserializeList<DiscordAuditLogEntry>("audit_log_entries", options),
            Users = element.DeserializeList<DiscordUser>("users", options),
            Webhooks = element.DeserializeList<DiscordWebhook>("webhooks", options),
            ScheduledEvents = element.DeserializeList<DiscordScheduledEvent>("guild_scheduled_events", options),
            AutoModerationRules = element.DeserializeList<DiscordAutoModerationRule>("auto_moderation_rules",
                options),
            Integrations = element.DeserializeList<DiscordIntegration>("integrations", options),
            Threads = element.DeserializeList<DiscordChannel>("threads", options)
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordAuditLog value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord audit logs are read-only.");
}

public sealed class ThreadListingConverter : JsonConverter<ThreadListing>
{
    public override ThreadListing Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new ThreadListing(
            element.DeserializeList<DiscordChannel>("threads", options),
            element.DeserializeList<DiscordThreadMember>("members", options),
            element.Flag("has_more"));
    }

    public override void Write(Utf8JsonWriter writer, ThreadListing value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Thread listings are read-only.");
}

public sealed class DiscordCommandPermissionsConverter : JsonConverter<DiscordCommandPermissions>
{
    public override DiscordCommandPermissions Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordCommandPermissions
        {
            CommandId = element.RequireSnowflake("id"),
            ApplicationId = element.RequireSnowflake("application_id"),
            GuildId = element.RequireSnowflake("guild_id"),
            Permissions = ReadPermissions(element)
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordCommandPermissions value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var permission in value.Permissions)
        {
            writer.WriteStartObject();
            writer.WriteString("id", permission.Id.ToString());
            writer.WriteNumber("type", (int)permission.Target);
            writer.WriteBoolean("permission", permission.Allowed);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static IReadOnlyList<DiscordCommandPermission> ReadPermissions(JsonElement element)
    {
        if (element.Property("permissions") is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var permissions = new List<DiscordCommandPermission>(array.GetArrayLength());

        foreach (var permission in array.EnumerateArray())
            permissions.Add(new DiscordCommandPermission(
                permission.RequireSnowflake("id"),
                (CommandPermissionTarget)(permission.IntegerOrNull("type") ?? 1),
                permission.Flag("permission")));

        return permissions;
    }
}

public sealed class DiscordVoiceStateConverter : JsonConverter<DiscordVoiceState>
{
    public override DiscordVoiceState Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var member = element.Deserialize<DiscordMember>("member", options);

        return new DiscordVoiceState
        {
            UserId = element.SnowflakeOrNull("user_id") ?? member?.User.Id ??
                throw new JsonException("Expected a voice state to carry a user id."),
            GuildId = element.SnowflakeOrNull("guild_id") ?? member?.GuildId,
            ChannelId = element.SnowflakeOrNull("channel_id"),
            Member = member,
            SessionId = element.StringOrNull("session_id") ?? string.Empty,
            Deaf = element.Flag("deaf"),
            Mute = element.Flag("mute"),
            SelfDeaf = element.Flag("self_deaf"),
            SelfMute = element.Flag("self_mute"),
            SelfStream = element.Flag("self_stream"),
            SelfVideo = element.Flag("self_video"),
            Suppress = element.Flag("suppress"),
            RequestToSpeakAt = element.TimestampOrNull("request_to_speak_timestamp")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordVoiceState value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord voice states are read-only.");
}

public sealed class DiscordInviteConverter : JsonConverter<DiscordInvite>
{
    public override DiscordInvite Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordInvite
        {
            Code = element.StringOrNull("code") ?? throw new JsonException("Expected an invite to carry a code."),
            GuildId = element.SnowflakeOrNull("guild_id") ?? element.Property("guild")?.SnowflakeOrNull("id"),
            ChannelId = element.SnowflakeOrNull("channel_id") ?? element.Property("channel")?.SnowflakeOrNull("id"),
            Inviter = element.Deserialize<DiscordUser>("inviter", options),
            TargetUser = element.Deserialize<DiscordUser>("target_user", options),
            TargetType = (InviteTargetType)(element.IntegerOrNull("target_type") ?? 0),
            Uses = element.IntegerOrNull("uses") ?? 0,
            MaxUses = element.IntegerOrNull("max_uses") ?? 0,
            MaxAge = TimeSpan.FromSeconds(element.IntegerOrNull("max_age") ?? 0),
            Temporary = element.Flag("temporary"),
            CreatedAt = element.TimestampOrNull("created_at"),
            ExpiresAt = element.TimestampOrNull("expires_at"),
            ApproximateMemberCount = element.IntegerOrNull("approximate_member_count"),
            ApproximatePresenceCount = element.IntegerOrNull("approximate_presence_count")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordInvite value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord invites are read-only.");
}

public sealed class DiscordThreadMemberConverter : JsonConverter<DiscordThreadMember>
{
    public override DiscordThreadMember Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var member = element.Deserialize<DiscordMember>("member", options);

        return new DiscordThreadMember
        {
            ThreadId = element.SnowflakeOrNull("id"),
            UserId = element.SnowflakeOrNull("user_id") ?? member?.User.Id,
            GuildId = element.SnowflakeOrNull("guild_id") ?? member?.GuildId,
            JoinedAt = element.TimestampOrNull("join_timestamp"),
            Flags = element.IntegerOrNull("flags") ?? 0,
            Member = member
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordThreadMember value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord thread members are read-only.");
}

public sealed class DiscordStickerConverter : JsonConverter<DiscordSticker>
{
    public override DiscordSticker Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordSticker
        {
            Id = element.RequireSnowflake("id"),
            Name = element.StringOrNull("name") ?? string.Empty,
            Description = element.StringOrNull("description"),
            Tags = element.StringOrNull("tags") ?? string.Empty,
            PackId = element.SnowflakeOrNull("pack_id"),
            GuildId = element.SnowflakeOrNull("guild_id"),
            Type = (StickerType)(element.IntegerOrNull("type") ?? 2),
            FormatType = (StickerFormatType)(element.IntegerOrNull("format_type") ?? 1),
            Available = element.Property("available") is not { ValueKind: JsonValueKind.False },
            SortValue = element.IntegerOrNull("sort_value"),
            Author = element.Deserialize<DiscordUser>("user", options)
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordSticker value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord stickers are read-only.");
}

public sealed class DiscordAuditLogEntryConverter : JsonConverter<DiscordAuditLogEntry>
{
    public override DiscordAuditLogEntry Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var extra = element.Property("options");

        return new DiscordAuditLogEntry
        {
            Id = element.RequireSnowflake("id"),
            GuildId = element.SnowflakeOrNull("guild_id"),
            TargetId = element.SnowflakeOrNull("target_id"),
            UserId = element.SnowflakeOrNull("user_id"),
            Action = ReadAction(element),
            Reason = element.StringOrNull("reason"),
            Changes = ReadChanges(element),
            ChannelId = extra?.SnowflakeOrNull("channel_id"),
            MessageId = extra?.SnowflakeOrNull("message_id"),
            RoleId = extra?.SnowflakeOrNull("role_id") ?? extra?.SnowflakeOrNull("id"),
            RoleName = extra?.StringOrNull("role_name"),
            Count = extra?.IntegerOrNull("count"),
            DeleteMemberDays = extra?.IntegerOrNull("delete_member_days"),
            MembersRemoved = extra?.IntegerOrNull("members_removed"),
            AutoModerationRuleName = extra?.StringOrNull("auto_moderation_rule_name"),
            AutoModerationTriggerType = extra?.StringOrNull("auto_moderation_rule_trigger_type")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordAuditLogEntry value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord audit log entries are read-only.");

    private static AuditLogAction ReadAction(JsonElement element)
    {
        var raw = element.IntegerOrNull("action_type");

        return raw is { } value && Enum.IsDefined(typeof(AuditLogAction), value)
            ? (AuditLogAction)value
            : AuditLogAction.Unknown;
    }

    private static IReadOnlyList<DiscordAuditLogChange> ReadChanges(JsonElement element)
    {
        if (element.Property("changes") is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var changes = new List<DiscordAuditLogChange>(array.GetArrayLength());

        foreach (var change in array.EnumerateArray())
        {
            if (change.StringOrNull("key") is not { } key)
                continue;

            changes.Add(new DiscordAuditLogChange(
                key,
                change.Property("old_value")?.Clone(),
                change.Property("new_value")?.Clone()));
        }

        return changes;
    }
}

public sealed class AutoModerationActionConverter : JsonConverter<AutoModerationAction>
{
    public override AutoModerationAction Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var metadata = element.Property("metadata");

        return new AutoModerationAction
        {
            Type = (AutoModerationActionType)(element.IntegerOrNull("type") ?? 1),
            ChannelId = metadata?.SnowflakeOrNull("channel_id"),
            Duration = metadata?.IntegerOrNull("duration_seconds") is { } seconds
                ? TimeSpan.FromSeconds(seconds)
                : null,
            CustomMessage = metadata?.StringOrNull("custom_message")
        };
    }

    public override void Write(Utf8JsonWriter writer, AutoModerationAction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("type", (int)value.Type);

        if (value.ChannelId is null && value.Duration is null && value.CustomMessage is null)
        {
            writer.WriteEndObject();

            return;
        }

        writer.WriteStartObject("metadata");

        if (value.ChannelId is { } channelId)
            writer.WriteString("channel_id", channelId.ToString());

        if (value.Duration is { } duration)
            writer.WriteNumber("duration_seconds", (int)duration.TotalSeconds);

        if (value.CustomMessage is { } message)
            writer.WriteString("custom_message", message);

        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

public sealed class AutoModerationTriggerMetadataConverter : JsonConverter<AutoModerationTriggerMetadata>
{
    public override AutoModerationTriggerMetadata Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new AutoModerationTriggerMetadata
        {
            KeywordFilter = element.StringList("keyword_filter"),
            RegexPatterns = element.StringList("regex_patterns"),
            Presets = ReadPresets(element),
            AllowList = element.StringList("allow_list"),
            MentionTotalLimit = element.IntegerOrNull("mention_total_limit"),
            MentionRaidProtectionEnabled = element.Flag("mention_raid_protection_enabled")
        };
    }

    public override void Write(Utf8JsonWriter writer, AutoModerationTriggerMetadata value,
        JsonSerializerOptions options) =>
        throw new NotSupportedException("Auto moderation trigger metadata is read-only.");

    private static IReadOnlyList<AutoModerationKeywordPreset> ReadPresets(JsonElement element)
    {
        if (element.Property("presets") is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var presets = new List<AutoModerationKeywordPreset>(array.GetArrayLength());

        foreach (var preset in array.EnumerateArray())
            if (preset.ValueKind is JsonValueKind.Number)
                presets.Add((AutoModerationKeywordPreset)preset.GetInt32());

        return presets;
    }
}

public sealed class DiscordAutoModerationRuleConverter : JsonConverter<DiscordAutoModerationRule>
{
    public override DiscordAutoModerationRule Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordAutoModerationRule
        {
            Id = element.RequireSnowflake("id"),
            GuildId = element.SnowflakeOrNull("guild_id"),
            Name = element.StringOrNull("name") ?? string.Empty,
            CreatorId = element.SnowflakeOrNull("creator_id"),
            EventType = (AutoModerationEventType)(element.IntegerOrNull("event_type") ?? 1),
            TriggerType = (AutoModerationTriggerType)(element.IntegerOrNull("trigger_type") ?? 1),
            TriggerMetadata = element.Deserialize<AutoModerationTriggerMetadata>("trigger_metadata", options),
            Actions = element.DeserializeList<AutoModerationAction>("actions", options),
            Enabled = element.Flag("enabled"),
            ExemptRoles = element.SnowflakeList("exempt_roles"),
            ExemptChannels = element.SnowflakeList("exempt_channels")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordAutoModerationRule value,
        JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord auto moderation rules are read-only.");
}

public sealed class DiscordScheduledEventConverter : JsonConverter<DiscordScheduledEvent>
{
    public override DiscordScheduledEvent Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordScheduledEvent
        {
            Id = element.RequireSnowflake("id"),
            GuildId = element.SnowflakeOrNull("guild_id"),
            ChannelId = element.SnowflakeOrNull("channel_id"),
            CreatorId = element.SnowflakeOrNull("creator_id"),
            Name = element.StringOrNull("name") ?? string.Empty,
            Description = element.StringOrNull("description"),
            Image = element.StringOrNull("image"),
            StartsAt = element.TimestampOrNull("scheduled_start_time") ?? default,
            EndsAt = element.TimestampOrNull("scheduled_end_time"),
            Status = (ScheduledEventStatus)(element.IntegerOrNull("status") ?? 1),
            EntityType = (ScheduledEventEntityType)(element.IntegerOrNull("entity_type") ?? 3),
            PrivacyLevel = (ScheduledEventPrivacyLevel)(element.IntegerOrNull("privacy_level") ?? 2),
            EntityId = element.SnowflakeOrNull("entity_id"),
            Location = element.Property("entity_metadata")?.StringOrNull("location"),
            Creator = element.Deserialize<DiscordUser>("creator", options),
            UserCount = element.IntegerOrNull("user_count")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordScheduledEvent value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord scheduled events are read-only.");
}

public sealed class DiscordStageInstanceConverter : JsonConverter<DiscordStageInstance>
{
    public override DiscordStageInstance Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordStageInstance
        {
            Id = element.RequireSnowflake("id"),
            GuildId = element.SnowflakeOrNull("guild_id"),
            ChannelId = element.RequireSnowflake("channel_id"),
            Topic = element.StringOrNull("topic") ?? string.Empty,
            PrivacyLevel = (StagePrivacyLevel)(element.IntegerOrNull("privacy_level") ?? 2),
            ScheduledEventId = element.SnowflakeOrNull("guild_scheduled_event_id")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordStageInstance value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord stage instances are read-only.");
}

public sealed class DiscordIntegrationConverter : JsonConverter<DiscordIntegration>
{
    public override DiscordIntegration Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var account = element.Property("account");

        return new DiscordIntegration
        {
            Id = element.RequireSnowflake("id"),
            GuildId = element.SnowflakeOrNull("guild_id"),
            Name = element.StringOrNull("name") ?? string.Empty,
            Type = element.StringOrNull("type") ?? string.Empty,
            Enabled = element.Flag("enabled"),
            Syncing = element.Flag("syncing"),
            Revoked = element.Flag("revoked"),
            EnableEmoticons = element.Flag("enable_emoticons"),
            RoleId = element.SnowflakeOrNull("role_id"),
            ExpireBehavior = (IntegrationExpireBehavior)(element.IntegerOrNull("expire_behavior") ?? 0),
            ExpireGracePeriod = element.IntegerOrNull("expire_grace_period"),
            User = element.Deserialize<DiscordUser>("user", options),
            Account = account is { } value
                ? new DiscordIntegrationAccount(value.StringOrNull("id") ?? string.Empty,
                    value.StringOrNull("name") ?? string.Empty)
                : null,
            SyncedAt = element.TimestampOrNull("synced_at"),
            SubscriberCount = element.IntegerOrNull("subscriber_count"),
            ApplicationId = element.Property("application")?.SnowflakeOrNull("id"),
            Scopes = element.StringList("scopes")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordIntegration value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord integrations are read-only.");
}

public sealed class DiscordEntitlementConverter : JsonConverter<DiscordEntitlement>
{
    public override DiscordEntitlement Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return new DiscordEntitlement
        {
            Id = element.RequireSnowflake("id"),
            SkuId = element.RequireSnowflake("sku_id"),
            ApplicationId = element.SnowflakeOrNull("application_id"),
            UserId = element.SnowflakeOrNull("user_id"),
            GuildId = element.SnowflakeOrNull("guild_id"),
            Type = (EntitlementType)(element.IntegerOrNull("type") ?? 1),
            Deleted = element.Flag("deleted"),
            Consumed = element.Flag("consumed"),
            StartsAt = element.TimestampOrNull("starts_at"),
            EndsAt = element.TimestampOrNull("ends_at")
        };
    }

    public override void Write(Utf8JsonWriter writer, DiscordEntitlement value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Discord entitlements are read-only.");
}

public sealed class MessageComponentConverter : JsonConverter<DiscordComponent>
{
    public override DiscordComponent Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);

        return ReadComponent(element, options);
    }

    public override void Write(Utf8JsonWriter writer, DiscordComponent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("type", (int)value.Type);

        switch (value)
        {
            case DiscordActionRow row:
                writer.WritePropertyName("components");
                writer.WriteStartArray();

                foreach (var child in row.Components)
                    Write(writer, child, options);

                writer.WriteEndArray();
                break;

            case DiscordButton button:
                WriteButton(writer, button, options);
                break;

            case DiscordSelectMenu select:
                WriteSelect(writer, select, options);
                break;

            case DiscordTextInput input:
                WriteTextInput(writer, input);
                break;
        }

        writer.WriteEndObject();
    }

    internal static IReadOnlyList<DiscordComponent> ReadList(JsonElement? element, JsonSerializerOptions options)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var components = new List<DiscordComponent>(array.GetArrayLength());

        foreach (var component in array.EnumerateArray())
            components.Add(ReadComponent(component, options));

        return components;
    }

    private static DiscordComponent ReadComponent(JsonElement element, JsonSerializerOptions options)
    {
        var type = (MessageComponentType)element.RequireInt32("type");

        return type switch
        {
            MessageComponentType.ActionRow => new DiscordActionRow
            {
                Components = ReadList(element.Property("components"), options)
            },
            MessageComponentType.Button => ReadButton(element, options),
            MessageComponentType.TextInput => ReadTextInput(element),
            _ => ReadSelect(element, type, options)
        };
    }

    private static DiscordButton ReadButton(JsonElement element, JsonSerializerOptions options) => new()
    {
        Style = (ButtonStyle)(element.Int32OrNull("style") ?? (int)ButtonStyle.Primary),
        Label = element.StringOrNull("label"),
        Emoji = element.Deserialize<DiscordEmoji>("emoji", options),
        Id = element.StringOrNull("custom_id"),
        Url = element.StringOrNull("url"),
        SkuId = element.SnowflakeOrNull("sku_id"),
        Disabled = element.Flag("disabled")
    };

    private static DiscordTextInput ReadTextInput(JsonElement element) => new()
    {
        Id = element.StringOrNull("custom_id") ?? string.Empty,
        Label = element.StringOrNull("label") ?? string.Empty,
        Style = (TextInputStyle)(element.Int32OrNull("style") ?? (int)TextInputStyle.Short),
        Value = element.StringOrNull("value"),
        Placeholder = element.StringOrNull("placeholder"),
        MinLength = element.Int32OrNull("min_length"),
        MaxLength = element.Int32OrNull("max_length"),
        Required = element.Property("required") is not { ValueKind: JsonValueKind.False }
    };

    private static DiscordSelectMenu ReadSelect(JsonElement element, MessageComponentType type,
        JsonSerializerOptions options) => new()
    {
        Kind = (SelectKind)type,
        Id = element.StringOrNull("custom_id") ?? string.Empty,
        Placeholder = element.StringOrNull("placeholder"),
        Options = ReadOptions(element.Property("options"), options),
        ChannelTypes = ReadChannelTypes(element.Property("channel_types")),
        DefaultValues = ReadDefaultValues(element.Property("default_values")),
        MinValues = element.Int32OrNull("min_values"),
        MaxValues = element.Int32OrNull("max_values"),
        Disabled = element.Flag("disabled")
    };

    private static IReadOnlyList<DiscordSelectOption> ReadOptions(JsonElement? element,
        JsonSerializerOptions options)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var parsed = new List<DiscordSelectOption>(array.GetArrayLength());

        foreach (var option in array.EnumerateArray())
            parsed.Add(new DiscordSelectOption(option.StringOrNull("label") ?? string.Empty,
                option.StringOrNull("value") ?? string.Empty)
            {
                Description = option.StringOrNull("description"),
                Emoji = option.Deserialize<DiscordEmoji>("emoji", options),
                Default = option.Flag("default")
            });

        return parsed;
    }

    private static IReadOnlyList<ChannelType> ReadChannelTypes(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var types = new List<ChannelType>(array.GetArrayLength());

        foreach (var type in array.EnumerateArray())
            types.Add((ChannelType)type.GetInt32());

        return types;
    }

    private static IReadOnlyList<DiscordSelectDefaultValue> ReadDefaultValues(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var values = new List<DiscordSelectDefaultValue>(array.GetArrayLength());

        foreach (var value in array.EnumerateArray())
            values.Add(new DiscordSelectDefaultValue(value.RequireSnowflake("id"),
                ParseDefaultValueType(value.StringOrNull("type"))));

        return values;
    }

    private static SelectDefaultValueType ParseDefaultValueType(string? type) => type switch
    {
        "role" => SelectDefaultValueType.Role,
        "channel" => SelectDefaultValueType.Channel,
        _ => SelectDefaultValueType.User
    };

    private static string NameOf(SelectDefaultValueType type) => type switch
    {
        SelectDefaultValueType.Role => "role",
        SelectDefaultValueType.Channel => "channel",
        _ => "user"
    };

    private static void WriteButton(Utf8JsonWriter writer, DiscordButton button, JsonSerializerOptions options)
    {
        writer.WriteNumber("style", (int)button.Style);

        if (button.Label is not null)
            writer.WriteString("label", button.Label);

        if (button.Emoji is { } emoji)
        {
            writer.WritePropertyName("emoji");
            JsonSerializer.Serialize(writer, emoji, options);
        }

        if (button.Id is not null)
            writer.WriteString("custom_id", button.Id);

        if (button.Url is not null)
            writer.WriteString("url", button.Url);

        if (button.SkuId is { } sku)
            writer.WriteString("sku_id", sku.ToString());

        if (button.Disabled)
            writer.WriteBoolean("disabled", true);
    }

    private static void WriteSelect(Utf8JsonWriter writer, DiscordSelectMenu select, JsonSerializerOptions options)
    {
        writer.WriteString("custom_id", select.Id);

        if (select.Placeholder is not null)
            writer.WriteString("placeholder", select.Placeholder);

        if (select.IsStringSelect)
        {
            writer.WritePropertyName("options");
            writer.WriteStartArray();

            foreach (var option in select.Options)
            {
                writer.WriteStartObject();
                writer.WriteString("label", option.Label);
                writer.WriteString("value", option.Value);

                if (option.Description is not null)
                    writer.WriteString("description", option.Description);

                if (option.Emoji is { } emoji)
                {
                    writer.WritePropertyName("emoji");
                    JsonSerializer.Serialize(writer, emoji, options);
                }

                if (option.Default)
                    writer.WriteBoolean("default", true);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        if (select.ChannelTypes.Count > 0)
        {
            writer.WritePropertyName("channel_types");
            writer.WriteStartArray();

            foreach (var channelType in select.ChannelTypes)
                writer.WriteNumberValue((int)channelType);

            writer.WriteEndArray();
        }

        if (select.DefaultValues.Count > 0)
        {
            writer.WritePropertyName("default_values");
            writer.WriteStartArray();

            foreach (var value in select.DefaultValues)
            {
                writer.WriteStartObject();
                writer.WriteString("id", value.Id.ToString());
                writer.WriteString("type", NameOf(value.Type));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        if (select.MinValues is { } min)
            writer.WriteNumber("min_values", min);

        if (select.MaxValues is { } max)
            writer.WriteNumber("max_values", max);

        if (select.Disabled)
            writer.WriteBoolean("disabled", true);
    }

    private static void WriteTextInput(Utf8JsonWriter writer, DiscordTextInput input)
    {
        writer.WriteString("custom_id", input.Id);
        writer.WriteString("label", input.Label);
        writer.WriteNumber("style", (int)input.Style);

        if (input.Value is not null)
            writer.WriteString("value", input.Value);

        if (input.Placeholder is not null)
            writer.WriteString("placeholder", input.Placeholder);

        if (input.MinLength is { } min)
            writer.WriteNumber("min_length", min);

        if (input.MaxLength is { } max)
            writer.WriteNumber("max_length", max);

        if (!input.Required)
            writer.WriteBoolean("required", false);
    }
}
