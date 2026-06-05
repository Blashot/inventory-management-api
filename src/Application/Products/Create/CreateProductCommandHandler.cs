using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Products;
using SharedKernel;

namespace Application.Products.Create;

public sealed class CreateProductCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        Result<Product> result = Product.Create(
            command.Name,
            command.Description,
            command.Price,
            command.Stock);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        Product product = result.Value;

        context.Products.Add(product);

        await context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

