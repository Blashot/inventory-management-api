using Domain.Orders;

namespace Domain.Pricing;

public interface IDiscountPolicy
{
    DiscountResult Calculate(PricingContext context);
}

