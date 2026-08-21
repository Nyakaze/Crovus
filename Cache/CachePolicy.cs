namespace Crovus.Cache;

public sealed record CachePolicy(int Capacity, TimeSpan? Lifetime = null)
{
    public static CachePolicy Disabled { get; } = new(0);

    public bool Enabled => Capacity > 0;
}
