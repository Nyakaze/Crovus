using Crovus.Cache;
using Crovus.Events;
using Crovus.Gateway;
using Crovus.Models;
using Crovus.Rest;
using Crovus.Services;

namespace Crovus.Client;

public interface ICrovusContext
{
    IDiscordRest Rest { get; }

    IDiscordCache Cache { get; }

    DiscordServices Services { get; }

    IDiscordGateway? Gateway { get; }

    PresenceTracker? Presences { get; }

    DiscordUser? CurrentUser { get; }

    Snowflake? ApplicationId { get; }
}
