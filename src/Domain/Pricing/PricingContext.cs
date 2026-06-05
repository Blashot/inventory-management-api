namespace Domain.Pricing;

/// <summary>
/// Carries all data that discount policies need to evaluate a potential discount.
/// Populated by <c>OrderPricingService</c> before dispatching to domain policies.
/// </summary>
public sealed record PricingContext(
    IReadOnlyList<PricedLine> Lines,
    decimal Subtotal,
    bool IsBlackFriday,
    bool IsHolidaySale);

