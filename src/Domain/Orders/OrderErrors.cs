using SharedKernel;

namespace Domain.Orders;

public static class OrderErrors
{
    public static readonly Error NoOrderLines =
        Error.Problem("Order.NoOrderLines", "An order must contain at least one order line.");
}

