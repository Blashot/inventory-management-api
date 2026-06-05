using Application.Abstractions.Holidays;
using Application.Abstractions.Pricing;
using Application.Pricing;
using Domain.Customers;
using Domain.Orders;
using Domain.Pricing;

namespace Application.UnitTests.Pricing;

public class OrderPricingServiceTests
{
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IHolidayCalendar _holidayCalendar = Substitute.For<IHolidayCalendar>();

    private OrderPricingService CreateService(bool isBlackFriday = false, bool isHolidaySale = false)
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        _holidayCalendar.IsBlackFriday(Arg.Any<DateTime>()).Returns(isBlackFriday);
        _holidayCalendar.IsHolidaySale(Arg.Any<DateTime>()).Returns(isHolidaySale);

        return new OrderPricingService(
        [
            new VolumeDiscountPolicy(),
            new BlackFridayDiscountPolicy(),
            new HolidayDiscountPolicy()
        ],
        _dateTimeProvider,
        _holidayCalendar);
    }

    // --- Regional pricing ---

    [Fact]
    public void Calculate_ForUSCustomer_ShouldNotApplyRegionalMultiplier()
    {
        OrderPricingService service = CreateService();
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, 1) };

        PricingResult result = service.Calculate(lines, Region.US);

        result.Lines[0].UnitPrice.ShouldBe(100m);
    }

    [Fact]
    public void Calculate_ForEuropeCustomer_ShouldApply15PercentIncrease()
    {
        OrderPricingService service = CreateService();
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, 1) };

        PricingResult result = service.Calculate(lines, Region.Europe);

        result.Lines[0].UnitPrice.ShouldBe(115m);
    }

    [Fact]
    public void Calculate_ForAsiaCustomer_ShouldApply5PercentIncrease()
    {
        OrderPricingService service = CreateService();
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, 1) };

        PricingResult result = service.Calculate(lines, Region.Asia);

        result.Lines[0].UnitPrice.ShouldBe(105m);
    }

    // --- Volume discounts ---

    [Theory]
    [InlineData(4, 0)]       // below threshold
    [InlineData(5, 10)]      // ≥5 → 10%
    [InlineData(9, 10)]      // still 10%
    [InlineData(10, 20)]     // ≥10 → 20%
    [InlineData(49, 20)]     // still 20%
    [InlineData(50, 30)]     // ≥50 → 30%
    public void Calculate_VolumeDiscount_ShouldApplyCorrectTier(int quantity, int expectedPct)
    {
        OrderPricingService service = CreateService();
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, quantity) };

        PricingResult result = service.Calculate(lines, Region.US);

        decimal expectedDiscount = 100m * quantity * expectedPct / 100m;
        result.Discount.Amount.ShouldBe(expectedDiscount);
    }

    // --- Black Friday ---

    [Fact]
    public void Calculate_OnBlackFriday_ShouldApply25PercentOnEntireOrder()
    {
        OrderPricingService service = CreateService(isBlackFriday: true);
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, 1) };

        PricingResult result = service.Calculate(lines, Region.US);

        result.Discount.Amount.ShouldBe(25m);
        result.Discount.Type.ShouldBe(DiscountType.BlackFriday);
    }

    // --- Holiday Sale ---

    [Fact]
    public void Calculate_OnHolidaySale_ShouldApply15PercentOnMostExpensiveProduct()
    {
        OrderPricingService service = CreateService(isHolidaySale: true);
        var cheapId = Guid.NewGuid();
        var expensiveId = Guid.NewGuid();
        var lines = new List<OrderLineRequest>
        {
            new(cheapId,     "Cheap",     10m, 1),
            new(expensiveId, "Expensive", 200m, 1)
        };

        PricingResult result = service.Calculate(lines, Region.US);

        // 15% of the expensive product's line total (200 * 1 = 200 → 15% = 30)
        result.Discount.Amount.ShouldBe(30m);
        result.Discount.Type.ShouldBe(DiscountType.HolidaySale);
    }

    // --- Best discount wins / no combining ---

    [Fact]
    public void Calculate_WhenMultipleDiscountsApply_ShouldSelectBestForCustomer()
    {
        // 10 units at $100 = $1000 subtotal
        // Volume: 20% of $1000 = $200
        // Black Friday: 25% of $1000 = $250  ← best
        OrderPricingService service = CreateService(isBlackFriday: true);
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, 10) };

        PricingResult result = service.Calculate(lines, Region.US);

        result.Discount.Amount.ShouldBe(250m);
        result.Discount.Type.ShouldBe(DiscountType.BlackFriday);
    }

    [Fact]
    public void Calculate_WhenMultipleDiscountsApply_ShouldNotCombineDiscounts()
    {
        // Volume (20% of $1000 = $200) AND Black Friday (25% of $1000 = $250) both qualify.
        // Combined would be $450 but only $250 should be applied.
        OrderPricingService service = CreateService(isBlackFriday: true);
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, 10) };

        PricingResult result = service.Calculate(lines, Region.US);

        result.Discount.Amount.ShouldBe(250m); // only the single best discount
    }

    [Fact]
    public void Calculate_WhenNoDiscountApplies_ShouldReturnNoneDiscount()
    {
        OrderPricingService service = CreateService();
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 10m, 1) };

        PricingResult result = service.Calculate(lines, Region.US);

        result.Discount.Type.ShouldBe(DiscountType.None);
        result.Discount.Amount.ShouldBe(0m);
    }

    // --- Policy orchestration ---

    [Fact]
    public void Calculate_Always_EvaluatesAllRegisteredPolicies()
    {
        IDiscountPolicy policy1 = Substitute.For<IDiscountPolicy>();
        IDiscountPolicy policy2 = Substitute.For<IDiscountPolicy>();
        policy1.Calculate(Arg.Any<PricingContext>()).Returns(DiscountResult.None);
        policy2.Calculate(Arg.Any<PricingContext>()).Returns(DiscountResult.None);

        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        _holidayCalendar.IsBlackFriday(Arg.Any<DateTime>()).Returns(false);
        _holidayCalendar.IsHolidaySale(Arg.Any<DateTime>()).Returns(false);

        var service = new OrderPricingService([policy1, policy2], _dateTimeProvider, _holidayCalendar);
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 100m, 1) };

        service.Calculate(lines, Region.US);

        policy1.Received(1).Calculate(Arg.Any<PricingContext>());
        policy2.Received(1).Calculate(Arg.Any<PricingContext>());
    }

    // --- IDateTimeProvider / IHolidayCalendar usage ---

    [Fact]
    public void Calculate_Always_AccessesCurrentDateFromDateTimeProvider()
    {
        OrderPricingService service = CreateService();
        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 10m, 1) };

        service.Calculate(lines, Region.US);

        _ = _dateTimeProvider.Received(1).UtcNow;
    }

    [Fact]
    public void Calculate_Always_ConsultsHolidayCalendarWithCurrentDate()
    {
        DateTime fixedDate = new(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(fixedDate);
        _holidayCalendar.IsBlackFriday(Arg.Any<DateTime>()).Returns(false);
        _holidayCalendar.IsHolidaySale(Arg.Any<DateTime>()).Returns(false);

        var service = new OrderPricingService(
            [new VolumeDiscountPolicy(), new BlackFridayDiscountPolicy(), new HolidayDiscountPolicy()],
            _dateTimeProvider,
            _holidayCalendar);

        var lines = new List<OrderLineRequest> { new(Guid.NewGuid(), "Widget", 10m, 1) };

        service.Calculate(lines, Region.US);

        _holidayCalendar.Received(1).IsBlackFriday(fixedDate);
        _holidayCalendar.Received(1).IsHolidaySale(fixedDate);
    }
}

