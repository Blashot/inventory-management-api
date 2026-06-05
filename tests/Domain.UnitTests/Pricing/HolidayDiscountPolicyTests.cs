using Domain.Orders;
using Domain.Pricing;

namespace Domain.UnitTests.Pricing;

public class HolidayDiscountPolicyTests
{
    private readonly HolidayDiscountPolicy _policy = new();

    private static PricedLine Line(decimal unitPrice, int quantity = 1) =>
        new(Guid.NewGuid(), "Product", unitPrice, quantity, unitPrice * quantity);

    private static PricingContext BuildContext(bool isHolidaySale, IReadOnlyList<PricedLine> lines)
    {
        decimal subtotal = lines.Sum(l => l.LineTotal);
        return new PricingContext(lines, subtotal, IsBlackFriday: false, IsHolidaySale: isHolidaySale);
    }

    [Fact]
    public void Calculate_OnNonHolidaySale_ShouldReturnNoDiscount()
    {
        PricingContext context = BuildContext(isHolidaySale: false, lines: [Line(100m)]);

        DiscountResult result = _policy.Calculate(context);

        result.ShouldBe(DiscountResult.None);
    }

    [Fact]
    public void Calculate_OnHolidaySale_WithEmptyLines_ShouldReturnNoDiscount()
    {
        PricingContext context = BuildContext(isHolidaySale: true, lines: []);

        DiscountResult result = _policy.Calculate(context);

        result.ShouldBe(DiscountResult.None);
    }

    [Fact]
    public void Calculate_OnHolidaySale_ShouldReturn15PercentOfMostExpensiveLineTotal()
    {
        // Most expensive by unit price = $200 line (qty 1 → line total $200)
        // 15% of $200 = $30
        IReadOnlyList<PricedLine> lines =
        [
            Line(unitPrice: 50m,  quantity: 2),  // line total 100
            Line(unitPrice: 200m, quantity: 1)   // line total 200 ← most expensive
        ];
        PricingContext context = BuildContext(isHolidaySale: true, lines: lines);

        DiscountResult result = _policy.Calculate(context);

        result.Type.ShouldBe(DiscountType.HolidaySale);
        result.Percentage.ShouldBe(0.15m);
        result.Amount.ShouldBe(30m); // 15% of 200
    }

    [Fact]
    public void Calculate_OnHolidaySale_WhenMostExpensiveHasMultipleUnits_ShouldBaseDiscountOnItsLineTotal()
    {
        // Most expensive unit price = $100 with qty=3 → line total $300
        // 15% of $300 = $45
        IReadOnlyList<PricedLine> lines =
        [
            Line(unitPrice: 100m, quantity: 3),  // line total 300 ← highest unit price
            Line(unitPrice: 80m,  quantity: 5)   // line total 400 (higher total, but lower unit price)
        ];
        PricingContext context = BuildContext(isHolidaySale: true, lines: lines);

        DiscountResult result = _policy.Calculate(context);

        result.Type.ShouldBe(DiscountType.HolidaySale);
        result.Amount.ShouldBe(45m); // 15% of 300 (line total of highest unit-price line)
    }

    [Fact]
    public void Calculate_OnHolidaySale_WithSingleLine_ShouldApplyTo15PercentOfThatLine()
    {
        PricingContext context = BuildContext(isHolidaySale: true, lines: [Line(unitPrice: 120m, quantity: 2)]);

        DiscountResult result = _policy.Calculate(context);

        result.Amount.ShouldBe(36m); // 15% of (120 * 2 = 240)
    }
}

