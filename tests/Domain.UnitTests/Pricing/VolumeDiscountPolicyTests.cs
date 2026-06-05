using Domain.Orders;
using Domain.Pricing;

namespace Domain.UnitTests.Pricing;

public class VolumeDiscountPolicyTests
{
    private readonly VolumeDiscountPolicy _policy = new();

    private static PricingContext BuildContext(IReadOnlyList<PricedLine> lines)
    {
        decimal subtotal = lines.Sum(l => l.LineTotal);
        return new PricingContext(lines, subtotal, IsBlackFriday: false, IsHolidaySale: false);
    }

    private static PricedLine Line(int quantity, decimal unitPrice = 10m) =>
        new(Guid.NewGuid(), "Product", unitPrice, quantity, unitPrice * quantity);

    // --- Threshold tiers ---

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void Calculate_WithLessThan5Units_ShouldReturnNoDiscount(int quantity)
    {
        PricingContext context = BuildContext([Line(quantity)]);

        DiscountResult result = _policy.Calculate(context);

        result.ShouldBe(DiscountResult.None);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(9)]
    public void Calculate_With5To9Units_ShouldReturn10PercentDiscount(int quantity)
    {
        PricingContext context = BuildContext([Line(quantity, unitPrice: 100m)]);

        DiscountResult result = _policy.Calculate(context);

        result.Type.ShouldBe(DiscountType.Volume);
        result.Percentage.ShouldBe(0.10m);
        result.Amount.ShouldBe(quantity * 100m * 0.10m);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(49)]
    public void Calculate_With10To49Units_ShouldReturn20PercentDiscount(int quantity)
    {
        PricingContext context = BuildContext([Line(quantity, unitPrice: 100m)]);

        DiscountResult result = _policy.Calculate(context);

        result.Type.ShouldBe(DiscountType.Volume);
        result.Percentage.ShouldBe(0.20m);
        result.Amount.ShouldBe(quantity * 100m * 0.20m);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    public void Calculate_With50OrMoreUnits_ShouldReturn30PercentDiscount(int quantity)
    {
        PricingContext context = BuildContext([Line(quantity, unitPrice: 100m)]);

        DiscountResult result = _policy.Calculate(context);

        result.Type.ShouldBe(DiscountType.Volume);
        result.Percentage.ShouldBe(0.30m);
        result.Amount.ShouldBe(quantity * 100m * 0.30m);
    }

    // --- Multiple lines: total quantity counts ---

    [Fact]
    public void Calculate_WithMultipleLines_ShouldSumQuantitiesAcrossLines()
    {
        // 3 + 4 = 7 units total → 10% tier
        PricingContext context = BuildContext(
        [
            Line(3, unitPrice: 100m),
            Line(4, unitPrice: 50m)
        ]);

        DiscountResult result = _policy.Calculate(context);

        result.Type.ShouldBe(DiscountType.Volume);
        result.Percentage.ShouldBe(0.10m);
        // 10% of subtotal (300 + 200 = 500)
        result.Amount.ShouldBe(50m);
    }

    // --- Highest applicable tier wins ---

    [Fact]
    public void Calculate_Always_SelectsHighestApplicableTier()
    {
        // 10 units → 20%, not 10%
        PricingContext context = BuildContext([Line(10, unitPrice: 100m)]);

        DiscountResult result = _policy.Calculate(context);

        result.Percentage.ShouldBe(0.20m);
    }
}

