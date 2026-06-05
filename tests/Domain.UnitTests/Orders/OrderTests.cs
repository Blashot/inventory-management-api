using Domain.Orders;

namespace Domain.UnitTests.Orders;

public class OrderTests
{
    private static (Guid ProductId, string ProductName, decimal UnitPrice, int Quantity) Line(
        decimal price = 10m, int qty = 1) =>
        (Guid.NewGuid(), "Product", price, qty);

    [Fact]
    public void Create_WithNoLines_ShouldFail()
    {
        Result<Order> result = Order.Create(
            Guid.NewGuid(),
            [],
            DiscountResult.None,
            DateTime.UtcNow);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithSingleLine_ShouldComputeCorrectTotal()
    {
        var lines = new List<(Guid, string, decimal, int)> { Line(price: 20m, qty: 3) };

        Result<Order> result = Order.Create(Guid.NewGuid(), lines, DiscountResult.None, DateTime.UtcNow);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalAmount.ShouldBe(60m);   // 20 * 3
        result.Value.DiscountApplied.ShouldBe(0m);
    }

    [Fact]
    public void Create_WithMultipleLines_ShouldSumLineTotals()
    {
        var lines = new List<(Guid, string, decimal, int)>
        {
            Line(price: 10m, qty: 2),
            Line(price: 25m, qty: 1)
        };

        Result<Order> result = Order.Create(Guid.NewGuid(), lines, DiscountResult.None, DateTime.UtcNow);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalAmount.ShouldBe(45m);   // (10*2) + (25*1)
    }

    [Fact]
    public void Create_WithDiscount_ShouldSubtractDiscountFromTotal()
    {
        var lines = new List<(Guid, string, decimal, int)> { Line(price: 100m, qty: 1) };
        var discount = new DiscountResult(Amount: 25m, Type: DiscountType.BlackFriday, Percentage: 0.25m);

        Result<Order> result = Order.Create(Guid.NewGuid(), lines, discount, DateTime.UtcNow);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalAmount.ShouldBe(75m);
        result.Value.DiscountApplied.ShouldBe(25m);
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        var lines = new List<(Guid, string, decimal, int)> { Line() };

        Result<Order> result = Order.Create(Guid.NewGuid(), lines, DiscountResult.None, DateTime.UtcNow);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DomainEvents.ShouldContain(e => e is OrderCreatedDomainEvent);
    }

    [Fact]
    public void Create_ShouldPopulateOrderLines()
    {
        var productId = Guid.NewGuid();
        var lines = new List<(Guid, string, decimal, int)>
        {
            (productId, "Widget", 15m, 2)
        };

        Order order = Order.Create(Guid.NewGuid(), lines, DiscountResult.None, DateTime.UtcNow).Value;

        order.OrderLines.ShouldHaveSingleItem();
        order.OrderLines[0].ProductId.ShouldBe(productId);
        order.OrderLines[0].UnitPrice.ShouldBe(15m);
        order.OrderLines[0].Quantity.ShouldBe(2);
        order.OrderLines[0].LineTotal.ShouldBe(30m);
    }
}


