using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Products.Get;

public sealed class GetProductsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetProductsQuery, List<ProductResponse>>
{
    public async Task<Result<List<ProductResponse>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        List<ProductResponse> products = await context.Products
            .AsNoTracking()
            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Stock))
            .ToListAsync(cancellationToken);

        return products;
    }
}

