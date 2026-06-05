using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Customers;
using SharedKernel;

namespace Application.Customers.Create;

internal sealed class CreateCustomerCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(command.Name, command.Region);

        context.Customers.Add(customer);

        await context.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}

