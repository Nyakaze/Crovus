namespace Crovus.Factory;

public static class DiscordLimits
{
    public const int MessageContent = 2000;
    public const int MessageEmbeds = 10;
    public const int MessageFiles = 10;
    public const int AttachmentDescription = 1024;

    public const int MessageActionRows = 5;
    public const int ButtonsPerRow = 5;
    public const int ComponentCustomId = 100;
    public const int ButtonLabel = 80;
    public const int SelectOptions = 25;
    public const int SelectPlaceholder = 150;
    public const int SelectOptionLabel = 100;
    public const int SelectOptionValue = 100;
    public const int SelectOptionDescription = 100;
    public const int ModalTitle = 45;
    public const int ModalInputs = 5;
    public const int TextInputLabel = 45;
    public const int TextInputValue = 4000;
    public const int TextInputPlaceholder = 100;

    public const int EmbedTitle = 256;
    public const int EmbedDescription = 4096;
    public const int EmbedFields = 25;
    public const int EmbedFieldName = 256;
    public const int EmbedFieldValue = 1024;
    public const int EmbedFooterText = 2048;
    public const int EmbedAuthorName = 256;
    public const int EmbedTotal = 6000;

    public const int ChannelName = 100;
    public const int ChannelTopic = 1024;
    public const int ForumTopic = 4096;
    public const int MaxSlowmodeSeconds = 21600;
    public const int MaxVoiceUserLimit = 99;
    public const int MaxStageUserLimit = 10000;
    public const int MinBitrate = 8000;
    public const int MaxBitrate = 384000;
    public const int ForumAppliedTags = 5;

    public const int EmojiNameMin = 2;
    public const int EmojiName = 32;
    public const int EmojiImageBytes = 256 * 1024;

    public const int CommandName = 32;
    public const int CommandDescription = 100;
    public const int CommandOptions = 25;
    public const int CommandChoices = 25;
    public const int CommandChoiceName = 100;
    public const int CommandChoiceValue = 100;
    public const int CommandStringLength = 6000;
    public const int CommandTotal = 4000;

    public const int ThreadName = 100;
    public const int WebhookName = 80;
    public const int WebhookUsername = 80;

    public const int GuildName = 100;
    public const int Nickname = 32;
    public const int RoleName = 100;
    public const int MembersPerPage = 1000;
    public const int BansPerPage = 1000;
    public const int MemberSearchLimit = 1000;
    public const int MaxBanDeleteSeconds = 604800;
    public const int MaxTimeoutDays = 28;
}

internal static class Limit
{
    public static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} must not be empty.", field);

        return Text(value, max, field)!;
    }

    public static string? Text(string? value, int max, string field)
    {
        if (value is not null && value.Length > max)
            throw new ArgumentException(
                $"{field} must be at most {max} characters but was {value.Length}.", field);

        return value;
    }

    public static int? Range(int? value, int min, int max, string field)
    {
        if (value is { } actual && (actual < min || actual > max))
            throw new ArgumentOutOfRangeException(field, actual, $"{field} must be between {min} and {max}.");

        return value;
    }

    public static void Count(int count, int max, string field)
    {
        if (count > max)
            throw new ArgumentException($"{field} must hold at most {max} entries but had {count}.", field);
    }
}
