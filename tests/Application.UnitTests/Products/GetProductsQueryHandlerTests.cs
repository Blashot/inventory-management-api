using Application.Abstractions.Data;
using Application.Products.Get;
using Domain.Products;

namespace Application.UnitTests.Products;

public class GetProductsQueryHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _handler = new GetProductsQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_WhenProductsExist_ShouldReturnMappedResponses()
    {
        List<Product> products =
        [
            Product.Create("Widget", "A widget", 10m, 5).Value,
            Product.Create("Gadget", "A gadget", 25m, 10).Value
        ];

        _context.Products.Returns(new TestDbSet<Product>(products));

        Result<List<ProductResponse>> result =
            await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(p => p.Name == "Widget" && p.Price == 10m);
        result.Value.ShouldContain(p => p.Name == "Gadget" && p.Price == 25m);
    }

    [Fact]
    public async Task Handle_WhenNoProducts_ShouldReturnEmptyList()
    {
        _context.Products.Returns(new TestDbSet<Product>([]));

        Result<List<ProductResponse>> result =
            await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}
