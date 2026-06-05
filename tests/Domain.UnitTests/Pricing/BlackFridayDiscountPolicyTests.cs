using Domain.Orders;
using Domain.Pricing;

namespace Domain.UnitTests.Pricing;

public class BlackFridayDiscountPolicyTests
{
    private readonly BlackFridayDiscountPolicy _policy = new();

    private static PricingContext BuildContext(bool isBlackFriday, decimal subtotal = 200m) =>
        new(
            Lines: [new PricedLine(Guid.NewGuid(), "Widget", 200m, 1, subtotal)],
            Subtotal: subtotal,
            IsBlackFriday: isBlackFriday,
            IsHolidaySale: false);

    [Fact]
    public void Calculate_OnBlackFriday_ShouldReturn25PercentOfSubtotal()
    {
        PricingContext context = BuildContext(isBlackFriday: true, subtotal: 200m);

        DiscountResult result = _policy.Calculate(context);

        result.Type.ShouldBe(DiscountType.BlackFriday);
        result.Percentage.ShouldBe(0.25m);
        result.Amount.ShouldBe(50m); // 25% of 200
    }

    [Fact]
    public void Calculate_OnBlackFriday_ShouldRoundAmountCorrectly()
    {
        // 25% of 133.33 = 33.3325 → rounds to 33.33
        PricingContext context = BuildContext(isBlackFriday: true, subtotal: 133.33m);

        DiscountResult result = _policy.Calculate(context);

        result.Amount.ShouldBe(33.33m);
    }

    [Fact]
    public void Calculate_OnNonBlackFriday_ShouldReturnNoDiscount()
    {
        PricingContext context = BuildContext(isBlackFriday: false, subtotal: 200m);

        DiscountResult result = _policy.Calculate(context);

        result.ShouldBe(DiscountResult.None);
    }
}

