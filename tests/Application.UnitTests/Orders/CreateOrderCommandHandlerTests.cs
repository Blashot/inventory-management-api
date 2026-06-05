using Application.Abstractions.Data;
using Application.Abstractions.Pricing;
using Application.Orders.Create;
using Domain.Customers;
using Domain.Orders;
using Domain.Pricing;
using Domain.Products;

namespace Application.UnitTests.Orders;

public class CreateOrderCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IOrderPricingService _pricingService = Substitute.For<IOrderPricingService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc));
        _handler = new CreateOrderCommandHandler(_context, _pricingService, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldReturnNotFoundError()
    {
        _context.Customers.Returns(new TestDbSet<Customer>([]));

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new OrderLineInput(Guid.NewGuid(), 1)]);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFoundError()
    {
        var customer = Customer.Create("John", Region.US);
        _context.Customers.Returns(new TestDbSet<Customer>([customer]));
        _context.Products.Returns(new TestDbSet<Product>([]));

        var command = new CreateOrderCommand(
            customer.Id,
            [new OrderLineInput(Guid.NewGuid(), 1)]);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ShouldReturnConflictError()
    {
        var customer = Customer.Create("John", Region.US);
        Product product = Product.Create("Widget", "desc", 10m, 2).Value;

        _context.Customers.Returns(new TestDbSet<Customer>([customer]));
        _context.Products.Returns(new TestDbSet<Product>([product]));

        _pricingService
            .Calculate(Arg.Any<IReadOnlyList<OrderLineRequest>>(), Arg.Any<Region>())
            .Returns(new PricingResult(
                [new PricedLine(product.Id, product.Name, 10m, 5, 50m)],
                50m,
                DiscountResult.None));

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 5)]);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOrderAndReturnId()
    {
        var customer = Customer.Create("John", Region.US);
        Product product = Product.Create("Widget", "desc", 10m, 10).Value;

        _context.Customers.Returns(new TestDbSet<Customer>([customer]));
        _context.Products.Returns(new TestDbSet<Product>([product]));
        _context.Orders.Returns(new TestDbSet<Order>([]));
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        _pricingService
            .Calculate(Arg.Any<IReadOnlyList<OrderLineRequest>>(), Region.US)
            .Returns(new PricingResult(
                [new PricedLine(product.Id, product.Name, 10m, 3, 30m)],
                30m,
                DiscountResult.None));

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 3)]);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        product.Stock.ShouldBe(7); // 10 - 3
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistOrderWithCalculatedTotalAndDiscount()
    {
        var customer = Customer.Create("John", Region.US);
        Product product = Product.Create("Widget", "desc", 10m, 10).Value;
        var ordersSet = new TestDbSet<Order>([]);

        _context.Customers.Returns(new TestDbSet<Customer>([customer]));
        _context.Products.Returns(new TestDbSet<Product>([product]));
        _context.Orders.Returns(ordersSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var discount = new DiscountResult(Amount: 6m, Type: DiscountType.Volume, Percentage: 0.20m);
        _pricingService
            .Calculate(Arg.Any<IReadOnlyList<OrderLineRequest>>(), Region.US)
            .Returns(new PricingResult(
                [new PricedLine(product.Id, product.Name, 10m, 3, 30m)],
                30m,
                discount));

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 3)]);

        await _handler.Handle(command, CancellationToken.None);

        Order savedOrder = ordersSet.Entities[0];
        savedOrder.TotalAmount.ShouldBe(24m);    // 30 - 6
        savedOrder.DiscountApplied.ShouldBe(6m);
    }

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldNotPersistChanges()
    {
        _context.Customers.Returns(new TestDbSet<Customer>([]));

        var command = new CreateOrderCommand(Guid.NewGuid(), [new OrderLineInput(Guid.NewGuid(), 1)]);

        await _handler.Handle(command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldNotPersistChanges()
    {
        var customer = Customer.Create("John", Region.US);
        _context.Customers.Returns(new TestDbSet<Customer>([customer]));
        _context.Products.Returns(new TestDbSet<Product>([]));

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(Guid.NewGuid(), 1)]);

        await _handler.Handle(command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ShouldNotPersistChanges()
    {
        var customer = Customer.Create("John", Region.US);
        Product product = Product.Create("Widget", "desc", 10m, 2).Value;

        _context.Customers.Returns(new TestDbSet<Customer>([customer]));
        _context.Products.Returns(new TestDbSet<Product>([product]));

        _pricingService
            .Calculate(Arg.Any<IReadOnlyList<OrderLineRequest>>(), Arg.Any<Region>())
            .Returns(new PricingResult(
                [new PricedLine(product.Id, product.Name, 10m, 5, 50m)],
                50m,
                DiscountResult.None));

        var command = new CreateOrderCommand(customer.Id, [new OrderLineInput(product.Id, 5)]);

        await _handler.Handle(command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
