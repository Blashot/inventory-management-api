using SharedKernel;

namespace Domain.Products;

public sealed class Product : Entity
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public int Stock { get; private set; }

    private Product() { }

    public static Result<Product> Create(string name, string description, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(ProductErrors.NameRequired);
        }

        if (name.Length > 50)
        {
            return Result.Failure<Product>(ProductErrors.NameTooLong);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Failure<Product>(ProductErrors.DescriptionRequired);
        }

        if (description.Length > 50)
        {
            return Result.Failure<Product>(ProductErrors.DescriptionTooLong);
        }

        if (price <= 0)
        {
            return Result.Failure<Product>(ProductErrors.InvalidPrice);
        }

        if (stock < 0)
        {
            return Result.Failure<Product>(ProductErrors.InvalidStock);
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            Stock = stock
        };

        return product;
    }

    public Result ReduceStock(int quantity)
    {
        if (Stock - quantity < 0)
        {
            return Result.Failure(ProductErrors.InsufficientStock(Id, quantity, Stock));
        }

        Stock -= quantity;

        return Result.Success();
    }
}

