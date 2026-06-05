using Application.Abstractions.Messaging;
using Application.Products.Create;
using Application.Products.Get;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Products;

internal sealed class ProductEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("products").WithTags(Tags.Products);

        group.MapPost("", CreateProduct)
            .WithName("CreateProduct")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("", GetProducts)
            .WithName("GetProducts")
            .Produces<List<ProductResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        ICommandHandler<CreateProductCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.Stock);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/products/{result.Value}", new { id = result.Value })
            : CustomResults.Problem(result);
    }

    private static async Task<IResult> GetProducts(
        IQueryHandler<GetProductsQuery, List<ProductResponse>> handler,
        CancellationToken cancellationToken)
    {
        Result<List<ProductResponse>> result =
            await handler.Handle(new GetProductsQuery(), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : CustomResults.Problem(result);
    }
}

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock);

