using Crovus.Models;

namespace Crovus.Services;

public sealed record BroadcastFailure(Snowflake ChannelId, Exception Error);

public sealed record BroadcastResult(IReadOnlyList<DiscordMessage> Delivered, IReadOnlyList<BroadcastFailure> Failures)
{
    public static readonly BroadcastResult Empty = new([], []);

    public int Targets => Delivered.Count + Failures.Count;

    public bool Complete => Failures.Count == 0;
}

public sealed record PurgeResult(int Deleted, int Failed)
{
    public int Attempted => Deleted + Failed;

    public bool Complete => Failed == 0;
}

public sealed record CommandSyncResult(IReadOnlyList<DiscordApplicationCommand> Commands, int Added, int Changed,
    int Removed, int Unchanged)
{
    public bool HasChanges => Added > 0 || Changed > 0 || Removed > 0;
}
