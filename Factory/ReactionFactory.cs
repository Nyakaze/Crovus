using System.Diagnostics.CodeAnalysis;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Factory;

public sealed class ReactionFactory
{
    private readonly List<DiscordEmoji> _emojis = [];

    public static ReactionFactory Create() => new();

    public static ReactionFactory Of(params string[] emojis) => Create().Add(emojis);

    public int Count => _emojis.Count;

    public ReactionFactory Add(DiscordEmoji emoji)
    {
        ArgumentNullException.ThrowIfNull(emoji);

        if (!_emojis.Contains(emoji))
            _emojis.Add(emoji);

        return this;
    }

    public ReactionFactory Add(string emoji) => Add(Parse(emoji));

    public ReactionFactory Add(params string[] emojis)
    {
        ArgumentNullException.ThrowIfNull(emojis);

        foreach (var emoji in emojis)
            Add(emoji);

        return this;
    }

    public ReactionFactory AddWhen(bool condition, string emoji) => condition ? Add(emoji) : this;

    public ReactionFactory Remove(DiscordEmoji emoji)
    {
        _emojis.Remove(emoji);
        return this;
    }

    public ReactionFactory Clear()
    {
        _emojis.Clear();
        return this;
    }

    public IReadOnlyList<DiscordEmoji> Build() => _emojis.ToArray();

    public async Task ApplyAsync(IDiscordRest rest, Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        foreach (var emoji in _emojis)
            await rest.CreateReactionAsync(channelId, messageId, emoji, cancellationToken);
    }

    public Task ApplyAsync(IDiscordRest rest, DiscordMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return ApplyAsync(rest, message.ChannelId, message.Id, cancellationToken);
    }

    public async Task WithdrawAsync(IDiscordRest rest, Snowflake channelId, Snowflake messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rest);

        foreach (var emoji in _emojis)
            await rest.DeleteOwnReactionAsync(channelId, messageId, emoji, cancellationToken);
    }

    public static DiscordEmoji Unicode(string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            throw new ArgumentException("A unicode reaction must not be empty.", nameof(emoji));

        return new DiscordEmoji(emoji, null, false);
    }

    public static DiscordEmoji Custom(string name, Snowflake id, bool animated = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A custom reaction must have a name.", nameof(name));

        return new DiscordEmoji(name, id, animated);
    }

    public static DiscordEmoji Parse(string emoji) => EmojiParser.Parse(emoji);

    public static bool TryParse([NotNullWhen(true)] string? emoji, [NotNullWhen(true)] out DiscordEmoji? result) =>
        EmojiParser.TryParse(emoji, out result);

    public static string Format(DiscordEmoji emoji) => EmojiParser.Format(emoji);
}
