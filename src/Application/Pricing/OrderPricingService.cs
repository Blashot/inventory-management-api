using Application.Abstractions.Holidays;
using Application.Abstractions.Pricing;
using Domain.Customers;
using Domain.Orders;
using Domain.Pricing;
using SharedKernel;

namespace Application.Pricing;

public sealed class OrderPricingService(
    IEnumerable<IDiscountPolicy> discountPolicies,
    IDateTimeProvider dateTimeProvider,
    IHolidayCalendar holidayCalendar)
    : IOrderPricingService
{
    public PricingResult Calculate(IReadOnlyList<OrderLineRequest> lines, Region region)
    {
        decimal multiplier = region switch
        {
            Region.Europe => 1.15m,
            Region.Asia   => 1.05m,
            _             => 1.00m
        };

        var pricedLines = lines
            .Select(l =>
            {
                decimal unitPrice = Math.Round(l.BasePrice * multiplier, 2, MidpointRounding.AwayFromZero);
                decimal lineTotal = unitPrice * l.Quantity;
                return new PricedLine(l.ProductId, l.ProductName, unitPrice, l.Quantity, lineTotal);
            })
            .ToList();

        decimal subtotal = pricedLines.Sum(l => l.LineTotal);

        DateTime now = dateTimeProvider.UtcNow;
        bool isBlackFriday = holidayCalendar.IsBlackFriday(now);
        bool isHolidaySale = holidayCalendar.IsHolidaySale(now);

        var context = new PricingContext(pricedLines, subtotal, isBlackFriday, isHolidaySale);

        DiscountResult bestDiscount = discountPolicies
            .Select(p => p.Calculate(context))
            .MaxBy(d => d.Amount) ?? DiscountResult.None;

        return new PricingResult(pricedLines, subtotal, bestDiscount);
    }
}

