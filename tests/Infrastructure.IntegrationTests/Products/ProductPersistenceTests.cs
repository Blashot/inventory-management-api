using Application.Abstractions.Data;
using Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests.Products;

public class ProductPersistenceTests : BaseIntegrationTest
{
    [Fact]
    public async Task AddProduct_ShouldPersistToDatabase()
    {
        Product product = Product.Create("Widget", "A useful widget", 19.99m, 100).Value;

        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        Product? loaded = await DbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == product.Id);

        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe("Widget");
        loaded.Description.ShouldBe("A useful widget");
        loaded.Price.ShouldBe(19.99m);
        loaded.Stock.ShouldBe(100);
    }

    [Fact]
    public async Task ReduceStock_ShouldPersistUpdatedStock()
    {
        Product product = Product.Create("Widget", "desc", 10m, 20).Value;
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        Product toUpdate = await DbContext.Products.SingleAsync(p => p.Id == product.Id);
        toUpdate.ReduceStock(5);
        await DbContext.SaveChangesAsync();

        Product? loaded = await DbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == product.Id);

        loaded.ShouldNotBeNull();
        loaded.Stock.ShouldBe(15);
    }
}

