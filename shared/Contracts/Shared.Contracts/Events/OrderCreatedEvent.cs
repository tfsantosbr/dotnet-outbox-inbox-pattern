namespace Shared.Contracts.Events;

public record OrderCreatedEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTime CreatedOnUtc);
