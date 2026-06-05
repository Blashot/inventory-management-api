using Application.Abstractions.Messaging;

namespace Application.Products.Create;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock) : ICommand<Guid>;

