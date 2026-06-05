using Application.Abstractions.Messaging;
using Application.Customers.Create;
using Domain.Customers;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Customers;

internal sealed class CustomerEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("customers").WithTags(Tags.Customers);

        group.MapPost("", CreateCustomer)
            .WithName("CreateCustomer")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateCustomer(
        CreateCustomerRequest request,
        ICommandHandler<CreateCustomerCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(request.Name, request.Region);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/customers/{result.Value}", new { id = result.Value })
            : CustomResults.Problem(result);
    }
}

public sealed record CreateCustomerRequest(
    string Name,
    Region Region);

