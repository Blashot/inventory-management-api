using Application.Abstractions.Messaging;

namespace Application.Orders.Create;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyList<OrderLineInput> Lines) : ICommand<Guid>;

public sealed record OrderLineInput(Guid ProductId, int Quantity);

