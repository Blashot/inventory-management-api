using Application.Abstractions.Data;
using Application.Products.Create;
using Domain.Products;

namespace Application.UnitTests.Products;

public class CreateProductCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistProductAndReturnId()
    {
        var productsDbSet = new TestDbSet<Product>([]);
        _context.Products.Returns(productsDbSet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new CreateProductCommand("Widget", "A useful widget", 19.99m, 50);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        // Verify via the in-memory TestDbSet – not via NSubstitute, since TestDbSet is not a substitute
        productsDbSet.Entities.ShouldHaveSingleItem();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductDomainValidationFails_ShouldReturnFailure()
    {
        // Price = 0 violates domain invariant
        var command = new CreateProductCommand("Widget", "desc", 0m, 10);

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();

        _context.Products.DidNotReceive().Add(Arg.Any<Product>());
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
