using Domain.Orders;

namespace Domain.Pricing;

public sealed class HolidayDiscountPolicy : IDiscountPolicy
{
    public DiscountResult Calculate(PricingContext context)
    {
        if (!context.IsHolidaySale || context.Lines.Count == 0)
        {
            return DiscountResult.None;
        }

        PricedLine? mostExpensive = context.Lines.MaxBy(l => l.UnitPrice);

        if (mostExpensive is null)
        {
            return DiscountResult.None;
        }

        const decimal percentage = 0.15m;

        return new DiscountResult(
            Amount: Math.Round(mostExpensive.LineTotal * percentage, 2, MidpointRounding.AwayFromZero),
            Type: DiscountType.HolidaySale,
            Percentage: percentage);
    }
}

