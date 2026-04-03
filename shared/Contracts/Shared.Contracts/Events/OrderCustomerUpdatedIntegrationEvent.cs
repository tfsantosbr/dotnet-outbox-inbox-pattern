using Shared.Events;

namespace Shared.Contracts.Events;

public record OrderCustomerUpdatedIntegrationEvent : IntegrationEvent
{
    public OrderCustomerUpdatedIntegrationEvent(
        Guid orderId,
        Guid previousCustomerId,
        Guid newCustomerId,
        DateTime occurredOnUtc,
        string correlationId,
        string? causationId,
        string source)
        : base(occurredOnUtc, correlationId, causationId, source)
    {
        OrderId = orderId;
        PreviousCustomerId = previousCustomerId;
        NewCustomerId = newCustomerId;
    }

    public Guid OrderId { get; init; }
    public Guid PreviousCustomerId { get; init; }
    public Guid NewCustomerId { get; init; }
}
