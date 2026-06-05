using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Pricing;
using Domain.Customers;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Orders.Create;

public sealed class CreateOrderCommandHandler(
    IApplicationDbContext context,
    IOrderPricingService pricingService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        Customer? customer = await context.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<Guid>(CustomerErrors.NotFound(command.CustomerId));
        }

        var productIds = command.Lines.Select(l => l.ProductId).ToList();

        List<Product> products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (OrderLineInput line in command.Lines)
        {
            if (!products.Exists(p => p.Id == line.ProductId))
            {
                return Result.Failure<Guid>(ProductErrors.NotFound(line.ProductId));
            }
        }

        var pricingRequests = command.Lines
            .Select(l =>
            {
                Product product = products.First(p => p.Id == l.ProductId);
                return new OrderLineRequest(l.ProductId, product.Name, product.Price, l.Quantity);
            })
            .ToList();

        PricingResult pricing = pricingService.Calculate(pricingRequests, customer.Region);

        foreach (OrderLineInput line in command.Lines)
        {
            Product product = products.First(p => p.Id == line.ProductId);
            Result stockResult = product.ReduceStock(line.Quantity);

            if (stockResult.IsFailure)
            {
                return Result.Failure<Guid>(stockResult.Error);
            }
        }

        var orderLineData =
            pricing.Lines
                .Select(l => (l.ProductId, l.ProductName, l.UnitPrice, l.Quantity))
                .ToList();

        Result<Order> orderResult = Order.Create(
            command.CustomerId,
            orderLineData,
            pricing.Discount,
            dateTimeProvider.UtcNow);

        if (orderResult.IsFailure)
        {
            return Result.Failure<Guid>(orderResult.Error);
        }

        Order order = orderResult.Value;

        context.Orders.Add(order);

        await context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
