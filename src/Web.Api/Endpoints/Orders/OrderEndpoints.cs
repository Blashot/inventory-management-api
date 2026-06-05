using Application.Abstractions.Messaging;
using Application.Orders.Create;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Orders;

internal sealed class OrderEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("orders").WithTags(Tags.Orders);

        group.MapPost("", CreateOrder)
            .WithName("CreateOrder")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request,
        ICommandHandler<CreateOrderCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.CustomerId,
            request.Lines.Select(l => new OrderLineInput(l.ProductId, l.Quantity)).ToList());

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/orders/{result.Value}", new { id = result.Value })
            : CustomResults.Problem(result);
    }
}

public sealed record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyList<OrderLineRequest> Lines);

public sealed record OrderLineRequest(
    Guid ProductId,
    int Quantity);

