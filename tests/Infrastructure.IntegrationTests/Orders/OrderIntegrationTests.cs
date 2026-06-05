using Application.Abstractions.Messaging;
using Application.Abstractions.Pricing;
using Application.Orders.Create;
using Domain.Customers;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Orders;

public class OrderIntegrationTests : BaseIntegrationTest
{
    [Fact]
    public async Task CreateOrder_WithValidData_ShouldPersistOrderAndReduceStock()
    {
        // Arrange
        var customer = Customer.Create("Alice", Region.US);
        Product product = Product.Create("Widget", "desc", 10m, 20).Value;

        DbContext.Customers.Add(customer);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        ICommandHandler<CreateOrderCommand, Guid> handler = Scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateOrderCommand, Guid>>();

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 3)]);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Order? order = await DbContext.Orders
            .Include(o => o.OrderLines)
            .SingleOrDefaultAsync(o => o.Id == result.Value);

        order.ShouldNotBeNull();
        order.CustomerId.ShouldBe(customer.Id);
        order.OrderLines.Count.ShouldBe(1);
        order.TotalAmount.ShouldBe(30m); // 10 * 3

        Product? updatedProduct = await DbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == product.Id);

        updatedProduct.ShouldNotBeNull();
        updatedProduct.Stock.ShouldBe(17); // 20 - 3
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ShouldReturnConflictError()
    {
        var customer = Customer.Create("Bob", Region.Europe);
        Product product = Product.Create("Gadget", "desc", 50m, 2).Value;

        DbContext.Customers.Add(customer);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        ICommandHandler<CreateOrderCommand, Guid> handler = Scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateOrderCommand, Guid>>();

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 5)]);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);

        // Stock must remain unchanged
        Product? unchanged = await DbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == product.Id);

        unchanged.ShouldNotBeNull();
        unchanged.Stock.ShouldBe(2);
    }

    [Fact]
    public async Task CreateOrder_ForEuropeCustomer_ShouldApplyRegionalPricing()
    {
        var customer = Customer.Create("Carlos", Region.Europe);
        Product product = Product.Create("Widget", "desc", 100m, 10).Value;

        DbContext.Customers.Add(customer);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        ICommandHandler<CreateOrderCommand, Guid> handler = Scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateOrderCommand, Guid>>();

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 1)]);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Order? order = await DbContext.Orders
            .Include(o => o.OrderLines)
            .SingleOrDefaultAsync(o => o.Id == result.Value);

        order.ShouldNotBeNull();
        // Europe = +15% → unit price = 115, no discount (qty=1 < 5)
        order.OrderLines[0].UnitPrice.ShouldBe(115m);
        order.TotalAmount.ShouldBe(115m);
    }

    [Fact]
    public async Task CreateOrder_ShouldStoreProductNameAndUnitPriceSnapshotOnOrderLine()
    {
        // Product snapshot: name and unit price (after regional adjustment) are captured
        // at the time of ordering, so future product changes don't affect existing orders.
        var customer = Customer.Create("Diana", Region.US);
        Product product = Product.Create("Mechanical Keyboard", "desc", 80m, 10).Value;

        DbContext.Customers.Add(customer);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        ICommandHandler<CreateOrderCommand, Guid> handler = Scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateOrderCommand, Guid>>();

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 2)]);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Order? order = await DbContext.Orders
            .Include(o => o.OrderLines)
            .SingleOrDefaultAsync(o => o.Id == result.Value);

        order.ShouldNotBeNull();
        order.OrderLines.Count.ShouldBe(1);
        order.OrderLines[0].ProductName.ShouldBe("Mechanical Keyboard");
        order.OrderLines[0].UnitPrice.ShouldBe(80m);   // US: no regional adjustment
        order.OrderLines[0].Quantity.ShouldBe(2);
        order.OrderLines[0].LineTotal.ShouldBe(160m);  // 80 * 2
    }
}

