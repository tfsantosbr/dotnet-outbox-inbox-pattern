using Shared.Events;

namespace Shared.Contracts.Events;

public record OrderCreatedIntegrationEvent : IntegrationEvent
{
    public OrderCreatedIntegrationEvent(
        Guid orderId, 
        Guid customerId, 
        decimal totalAmount, 
        DateTime occurredOnUtc, 
        string correlationId, 
        string? causationId, 
        string source) 
        : base(occurredOnUtc, correlationId, causationId, source)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }

    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
}
