namespace Domain.Orders;

public sealed record DiscountResult(decimal Amount, DiscountType Type, decimal Percentage)
{
    public static readonly DiscountResult None = new(0m, DiscountType.None, 0m);
}

