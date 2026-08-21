using Crovus.Logs;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class DiscordServices : IAsyncDisposable
{
    public DiscordServices(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(rest);

        Rest = rest;
        Messages = new MessageService(rest, logger, telemetry);
        Embeds = new EmbedService(rest, logger, telemetry);
        Channels = new ChannelService(rest, logger, telemetry);
        Threads = new ThreadService(rest, logger, telemetry);
        Webhooks = new WebhookService(rest, logger, telemetry);
        Reactions = new ReactionService(rest, logger, telemetry);
        Emojis = new EmojiService(rest, logger, telemetry);
        Commands = new CommandService(rest, logger, telemetry);
        Interactions = new InteractionService(rest, logger, telemetry);
        Guilds = new GuildService(rest, logger, telemetry);
        Members = new MemberService(rest, logger, telemetry);
        Roles = new RoleService(rest, logger, telemetry);
    }

    public DiscordServices(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public IDiscordRest Rest { get; }

    public MessageService Messages { get; }

    public EmbedService Embeds { get; }

    public ChannelService Channels { get; }

    public ThreadService Threads { get; }

    public WebhookService Webhooks { get; }

    public ReactionService Reactions { get; }

    public EmojiService Emojis { get; }

    public CommandService Commands { get; }

    public InteractionService Interactions { get; }

    public GuildService Guilds { get; }

    public MemberService Members { get; }

    public RoleService Roles { get; }

    public ValueTask DisposeAsync() => Rest.DisposeAsync();
}
