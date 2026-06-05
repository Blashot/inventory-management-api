using Domain.Customers;
using Domain.Orders;
using Domain.Pricing;

namespace Application.Abstractions.Pricing;

public interface IOrderPricingService
{
    PricingResult Calculate(IReadOnlyList<OrderLineRequest> lines, Region region);
}

public sealed record OrderLineRequest(
    Guid ProductId,
    string ProductName,
    decimal BasePrice,
    int Quantity);


public sealed record PricingResult(
    IReadOnlyList<PricedLine> Lines,
    decimal Subtotal,
    DiscountResult Discount);

