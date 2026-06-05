using SharedKernel;

namespace Domain.Orders;

public sealed record OrderCreatedDomainEvent(Guid OrderId) : IDomainEvent;

