namespace Application.Products.Get;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock);

