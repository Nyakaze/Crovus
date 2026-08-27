namespace Crovus.Models;

[Flags]
public enum ActivityTypes
{
    None = 0,
    Playing = 1 << 0,
    Streaming = 1 << 1,
    ListeningTo = 1 << 2,
    Listening = ListeningTo,
    Watching = 1 << 3,
    Custom = 1 << 4,
    Competing = 1 << 5,
    ExceptCustom = Playing | Streaming | Listening | Watching | Competing,
    All = ExceptCustom | Custom
}

public static class ActivityTypeExtensions
{
    public static ActivityTypes ToFlag(this ActivityType type) => type switch
    {
        ActivityType.Playing => ActivityTypes.Playing,
        ActivityType.Streaming => ActivityTypes.Streaming,
        ActivityType.Listening => ActivityTypes.Listening,
        ActivityType.Watching => ActivityTypes.Watching,
        ActivityType.Custom => ActivityTypes.Custom,
        ActivityType.Competing => ActivityTypes.Competing,
        _ => ActivityTypes.None
    };

    public static bool Includes(this ActivityTypes types, ActivityType type) => (types & type.ToFlag()) != 0;

    public static bool Includes(this ActivityTypes types, DiscordActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return types.Includes(activity.Type);
    }

    public static IEnumerable<ActivityType> Enumerate(this ActivityTypes types)
    {
        if (types.Includes(ActivityType.Playing))
            yield return ActivityType.Playing;

        if (types.Includes(ActivityType.Streaming))
            yield return ActivityType.Streaming;

        if (types.Includes(ActivityType.Listening))
            yield return ActivityType.Listening;

        if (types.Includes(ActivityType.Watching))
            yield return ActivityType.Watching;

        if (types.Includes(ActivityType.Custom))
            yield return ActivityType.Custom;

        if (types.Includes(ActivityType.Competing))
            yield return ActivityType.Competing;
    }
}

public static class ActivityFilters
{
    public static IEnumerable<DiscordActivity> WithTypes(this IEnumerable<DiscordActivity> activities,
        ActivityTypes types)
    {
        ArgumentNullException.ThrowIfNull(activities);

        return activities.Where(activity => types.Includes(activity.Type));
    }

    public static IEnumerable<DiscordActivity> WithType(this IEnumerable<DiscordActivity> activities,
        ActivityType type)
    {
        ArgumentNullException.ThrowIfNull(activities);

        return activities.Where(activity => activity.Type == type);
    }

    public static IEnumerable<DiscordActivity> Named(this IEnumerable<DiscordActivity> activities, string name)
    {
        ArgumentNullException.ThrowIfNull(activities);

        return activities.Where(activity => string.Equals(activity.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<DiscordActivity> Playing(this IEnumerable<DiscordActivity> activities) =>
        activities.WithType(ActivityType.Playing);

    public static IEnumerable<DiscordActivity> Streaming(this IEnumerable<DiscordActivity> activities) =>
        activities.WithType(ActivityType.Streaming);

    public static IEnumerable<DiscordActivity> Listening(this IEnumerable<DiscordActivity> activities) =>
        activities.WithType(ActivityType.Listening);

    public static IEnumerable<DiscordActivity> ListeningTo(this IEnumerable<DiscordActivity> activities) =>
        activities.Listening();

    public static IEnumerable<DiscordActivity> Watching(this IEnumerable<DiscordActivity> activities) =>
        activities.WithType(ActivityType.Watching);

    public static IEnumerable<DiscordActivity> Competing(this IEnumerable<DiscordActivity> activities) =>
        activities.WithType(ActivityType.Competing);

    public static IEnumerable<DiscordActivity> CustomStatuses(this IEnumerable<DiscordActivity> activities) =>
        activities.WithType(ActivityType.Custom);

    public static bool HasAny(this IEnumerable<DiscordActivity> activities, ActivityTypes types)
    {
        ArgumentNullException.ThrowIfNull(activities);

        return activities.Any(activity => types.Includes(activity.Type));
    }

    public static ActivityTypes Types(this IEnumerable<DiscordActivity> activities)
    {
        ArgumentNullException.ThrowIfNull(activities);

        return activities.Aggregate(ActivityTypes.None, (types, activity) => types | activity.Type.ToFlag());
    }
}
