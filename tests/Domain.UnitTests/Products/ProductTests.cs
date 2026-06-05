using Domain.Products;

namespace Domain.UnitTests.Products;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        Result<Product> result = Product.Create("Widget", "A useful widget", 9.99m, 100);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Widget");
        result.Value.Description.ShouldBe("A useful widget");
        result.Value.Price.ShouldBe(9.99m);
        result.Value.Stock.ShouldBe(100);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldFail(string name)
    {
        Result<Product> result = Product.Create(name, "desc", 10m, 0);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithNameExceeding50Chars_ShouldFail()
    {
        Result<Product> result = Product.Create(new string('A', 51), "desc", 10m, 0);

        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDescription_ShouldFail(string description)
    {
        Result<Product> result = Product.Create("Widget", description, 10m, 0);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithDescriptionExceeding50Chars_ShouldFail()
    {
        Result<Product> result = Product.Create("Widget", new string('B', 51), 10m, 0);

        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_WithNonPositivePrice_ShouldFail(decimal price)
    {
        Result<Product> result = Product.Create("Widget", "desc", price, 0);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithNegativeStock_ShouldFail()
    {
        Result<Product> result = Product.Create("Widget", "desc", 10m, -1);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithZeroStock_ShouldSucceed()
    {
        Result<Product> result = Product.Create("Widget", "desc", 10m, 0);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Stock.ShouldBe(0);
    }

    [Fact]
    public void ReduceStock_WhenStockIsSufficient_ShouldDecreaseStock()
    {
        Product product = Product.Create("Widget", "desc", 10m, 10).Value;

        Result reduceResult = product.ReduceStock(3);

        reduceResult.IsSuccess.ShouldBeTrue();
        product.Stock.ShouldBe(7);
    }

    [Fact]
    public void ReduceStock_WhenExactStock_ShouldReduceToZero()
    {
        Product product = Product.Create("Widget", "desc", 10m, 5).Value;

        Result reduceResult = product.ReduceStock(5);

        reduceResult.IsSuccess.ShouldBeTrue();
        product.Stock.ShouldBe(0);
    }

    [Fact]
    public void ReduceStock_WhenStockInsufficient_ShouldFail()
    {
        Product product = Product.Create("Widget", "desc", 10m, 3).Value;

        Result reduceResult = product.ReduceStock(5);

        reduceResult.IsFailure.ShouldBeTrue();
        product.Stock.ShouldBe(3); // unchanged
    }
}

