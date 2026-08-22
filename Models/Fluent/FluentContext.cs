using Crovus.Cache;
using Crovus.Client;
using Crovus.Rest;
using Crovus.Services;

namespace Crovus.Models;

public static class FluentContext
{
    public static ICrovusContext Context(this IBoundEntity entity) => entity.RequireContext();

    public static DiscordServices Services(this IBoundEntity entity) => entity.RequireContext().Services;

    public static IDiscordRest Rest(this IBoundEntity entity) => entity.RequireContext().Rest;

    public static IDiscordCache Cache(this IBoundEntity entity) => entity.RequireContext().Cache;

    internal static Snowflake RequireApplicationId(this IBoundEntity entity) =>
        entity.RequireContext().ApplicationId ?? throw new InvalidOperationException(
            "The client does not know its application id yet. Connect the gateway, or use the " +
            "application-id overload on the service directly.");
}
