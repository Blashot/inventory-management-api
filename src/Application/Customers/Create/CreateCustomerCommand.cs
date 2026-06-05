using Application.Abstractions.Messaging;
using Domain.Customers;

namespace Application.Customers.Create;

public sealed record CreateCustomerCommand(string Name, Region Region) : ICommand<Guid>;

