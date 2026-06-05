using SharedKernel;

namespace Domain.Customers;

public static class CustomerErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Customer.NotFound", $"Customer with Id '{id}' was not found.");
}

