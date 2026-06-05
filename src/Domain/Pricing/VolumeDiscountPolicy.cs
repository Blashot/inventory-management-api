using Domain.Orders;

namespace Domain.Pricing;

public sealed class VolumeDiscountPolicy : IDiscountPolicy
{
    public DiscountResult Calculate(PricingContext context)
    {
        int totalUnits = context.Lines.Sum(l => l.Quantity);

        decimal percentage = totalUnits switch
        {
            >= 50 => 0.30m,
            >= 10 => 0.20m,
            >= 5  => 0.10m,
            _     => 0m
        };

        if (percentage == 0m)
        {
            return DiscountResult.None;
        }

        return new DiscountResult(
            Amount: Math.Round(context.Subtotal * percentage, 2, MidpointRounding.AwayFromZero),
            Type: DiscountType.Volume,
            Percentage: percentage);
    }
}

