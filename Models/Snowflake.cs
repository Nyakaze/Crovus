namespace Crovus.Models;

public readonly record struct Snowflake(ulong Value)
{
    public DateTimeOffset CreatedAt =>
        DateTimeOffset.FromUnixTimeMilliseconds((long)((Value >> 22) + 1420070400000UL));
    public override string ToString() => Value.ToString();
    public static implicit operator ulong(Snowflake s) => s.Value;
    public static implicit operator Snowflake(ulong v) => new(v);
}