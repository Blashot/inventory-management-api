using SharedKernel;

namespace Domain.Customers;

public sealed class Customer : Entity
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Region Region { get; private set; }

    private Customer() { }

    public static Customer Create(string name, Region region)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Region = region
        };

        return customer;
    }
}

