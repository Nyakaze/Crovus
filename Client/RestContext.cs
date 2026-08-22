using Crovus.Cache;
using Crovus.Events;
using Crovus.Gateway;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;
using Crovus.Services;

namespace Crovus.Client;

public sealed class RestContext : ICrovusContext
{
    private readonly Lazy<DiscordServices> _services;

    public RestContext(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(rest);

        Rest = rest;
        _services = new Lazy<DiscordServices>(() => new DiscordServices(rest, logger, telemetry));
    }

    public IDiscordRest Rest { get; }

    public IDiscordCache Cache => NullDiscordCache.Instance;

    public DiscordServices Services => _services.Value;

    public IDiscordGateway? Gateway => null;

    public PresenceTracker? Presences => null;

    public DiscordUser? CurrentUser => null;

    public Snowflake? ApplicationId => null;
}
