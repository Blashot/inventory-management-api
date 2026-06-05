using SharedKernel;

namespace Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<OrderLine> _orderLines = [];

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();

    public decimal TotalAmount { get; private set; }

    public decimal DiscountApplied { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Order() { }

    public static Result<Order> Create(
        Guid customerId,
        IReadOnlyList<(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity)> lines,
        DiscountResult discount,
        DateTime createdAt)
    {
        if (lines.Count == 0)
        {
            return Result.Failure<Order>(OrderErrors.NoOrderLines);
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedAt = createdAt
        };

        foreach ((Guid productId, string productName, decimal unitPrice, int quantity) in lines)
        {
            var line = OrderLine.Create(order.Id, productId, productName, unitPrice, quantity);
            order._orderLines.Add(line);
        }

        decimal subtotal = order._orderLines.Sum(l => l.LineTotal);
        order.DiscountApplied = discount.Amount;
        order.TotalAmount = subtotal - discount.Amount;

        order.Raise(new OrderCreatedDomainEvent(order.Id));

        return order;
    }
}



