using System.Diagnostics;
using Crovus.Queue;

namespace Crovus.Gateway;

internal sealed record GatewayCommand(
    GatewayOpcode Opcode,
    object? Payload,
    QueuePriority Priority,
    long Epoch,
    TaskCompletionSource? Completion,
    long EnqueuedAt)
{
    public static GatewayCommand Control(GatewayOpcode opcode, object? payload, long epoch) =>
        new(opcode, payload, QueuePriority.High, epoch, null, Stopwatch.GetTimestamp());

    public static GatewayCommand User(GatewayOpcode opcode, object? payload) =>
        new(opcode, payload, QueuePriority.Normal, AnyEpoch,
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), Stopwatch.GetTimestamp());

    public const long AnyEpoch = 0;

    public TimeSpan QueueLatency => Stopwatch.GetElapsedTime(EnqueuedAt);
}
