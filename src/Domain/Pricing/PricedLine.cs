namespace Domain.Pricing;

public sealed record PricedLine(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

