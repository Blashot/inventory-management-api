using Domain.Orders;

namespace Domain.Pricing;

public sealed class BlackFridayDiscountPolicy : IDiscountPolicy
{
    public DiscountResult Calculate(PricingContext context)
    {
        if (!context.IsBlackFriday)
        {
            return DiscountResult.None;
        }

        const decimal percentage = 0.25m;

        return new DiscountResult(
            Amount: Math.Round(context.Subtotal * percentage, 2, MidpointRounding.AwayFromZero),
            Type: DiscountType.BlackFriday,
            Percentage: percentage);
    }
}

