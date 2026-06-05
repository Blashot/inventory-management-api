using SharedKernel;

namespace Domain.Products;

public static class ProductErrors
{
    public static readonly Error NameRequired =
        Error.Problem("Product.NameRequired", "Product name is required.");

    public static readonly Error NameTooLong =
        Error.Problem("Product.NameTooLong", "Product name must not exceed 50 characters.");

    public static readonly Error DescriptionRequired =
        Error.Problem("Product.DescriptionRequired", "Product description is required.");

    public static readonly Error DescriptionTooLong =
        Error.Problem("Product.DescriptionTooLong", "Product description must not exceed 50 characters.");

    public static readonly Error InvalidPrice =
        Error.Problem("Product.InvalidPrice", "Product price must be greater than zero.");

    public static readonly Error InvalidStock =
        Error.Problem("Product.InvalidStock", "Product stock cannot be negative.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Product.NotFound", $"Product with Id '{id}' was not found.");

    public static Error InsufficientStock(Guid id, int requested, int available) =>
        Error.Conflict(
            "Product.InsufficientStock",
            $"Product '{id}' has insufficient stock. Requested: {requested}, Available: {available}.");
}

