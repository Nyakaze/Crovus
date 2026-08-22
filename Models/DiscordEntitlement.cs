using System.Text.Json.Serialization;
using Crovus.Client;

namespace Crovus.Models;

public enum EntitlementType
{
    Purchase = 1,
    PremiumSubscription = 2,
    DeveloperGift = 3,
    TestModePurchase = 4,
    FreePurchase = 5,
    UserGift = 6,
    PremiumPurchase = 7,
    ApplicationSubscription = 8
}

public sealed record DiscordEntitlement : IBoundEntity
{
    public required Snowflake Id { get; init; }

    public required Snowflake SkuId { get; init; }

    public Snowflake? ApplicationId { get; init; }

    public Snowflake? UserId { get; init; }

    public Snowflake? GuildId { get; init; }

    public EntitlementType Type { get; init; }

    public bool Deleted { get; init; }

    public bool Consumed { get; init; }

    public DateTimeOffset? StartsAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }

    [JsonIgnore]
    public bool IsSubscription => Type is EntitlementType.ApplicationSubscription or EntitlementType.PremiumSubscription;

    [JsonIgnore]
    public bool IsTest => Type is EntitlementType.TestModePurchase;

    [JsonIgnore]
    public bool IsActive => !Deleted &&
                            (StartsAt is not { } start || start <= DateTimeOffset.UtcNow) &&
                            (EndsAt is not { } end || end > DateTimeOffset.UtcNow);

    public override string ToString() => $"{Type} {SkuId}";

    private EntityBinding _binding;

    public DiscordEntitlement Bind(ICrovusContext context)
    {
        var bound = this with { };

        bound._binding = EntityBinding.To(context);

        return bound;
    }

    ICrovusContext? IBoundEntity.Context => _binding.Context;

    IBoundEntity IBoundEntity.WithContext(ICrovusContext context) => Bind(context);
}
