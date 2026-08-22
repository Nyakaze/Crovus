using System.Collections;
using Crovus.Client;

namespace Crovus.Models;

public static class EntityBinder
{
    public static T Bind<T>(T value, ICrovusContext? context)
    {
        if (context is null || value is null)
            return value;

        switch (value)
        {
            case IBoundEntity entity:
                return (T)entity.WithContext(context);

            case IList { IsReadOnly: false } list:
                for (var index = 0; index < list.Count; index++)
                    if (list[index] is IBoundEntity element)
                        list[index] = element.WithContext(context);

                return value;

            default:
                return value;
        }
    }

    public static IReadOnlyList<TEntity> BindAll<TEntity>(IReadOnlyList<TEntity> values, ICrovusContext context)
        where TEntity : IBoundEntity
    {
        if (values.Count == 0)
            return values;

        var bound = new TEntity[values.Count];

        for (var index = 0; index < values.Count; index++)
            bound[index] = (TEntity)values[index].WithContext(context);

        return bound;
    }
}
