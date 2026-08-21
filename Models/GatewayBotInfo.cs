using System.Text.Json.Serialization;

namespace Crovus.Models;

public sealed record SessionStartLimit(int Total, int Remaining, TimeSpan ResetAfter, int MaxConcurrency)
{
    [JsonIgnore]
    public bool IsExhausted => Remaining <= 0;

    [JsonIgnore]
    public DateTimeOffset ResetsAt => DateTimeOffset.UtcNow + ResetAfter;

    [JsonIgnore]
    public double UsedFraction => Total == 0 ? 0 : (Total - Remaining) / (double)Total;
}

public sealed record GatewayBotInfo
{
    public required string Url { get; init; }

    public int Shards { get; init; } = 1;

    public SessionStartLimit SessionStartLimit { get; init; } = new(1000, 1000, TimeSpan.Zero, 1);

    [JsonIgnore]
    public bool CanIdentify => !SessionStartLimit.IsExhausted;

    [JsonIgnore]
    public int MaxConcurrency => SessionStartLimit.MaxConcurrency;

    public IEnumerable<int> ShardIds => Enumerable.Range(0, Shards);

    public override string ToString() => $"{Url} ({Shards} shards)";
}
