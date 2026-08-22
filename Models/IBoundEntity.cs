using Crovus.Client;

namespace Crovus.Models;

public interface IBoundEntity
{
    ICrovusContext? Context { get; }

    IBoundEntity WithContext(ICrovusContext context);
}

public static class BoundEntity
{
    public static bool IsBound(this IBoundEntity entity) => entity.Context is not null;

    public static ICrovusContext RequireContext(this IBoundEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity.Context ?? throw new InvalidOperationException(
            $"This {entity.GetType().Name} is not bound to a client, so it cannot reach Discord. " +
            "Entities returned by CrovusClient, its services, its cache and its events are bound " +
            "automatically; ones you construct yourself are not - call Bind(client) on them first.");
    }
}
