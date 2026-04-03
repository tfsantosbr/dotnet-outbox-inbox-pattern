using Shared.Events;

namespace Shared.Contracts.Events;

public record OrderTotalAmountUpdatedIntegrationEvent : IntegrationEvent
{
    public OrderTotalAmountUpdatedIntegrationEvent(
        Guid orderId,
        decimal previousTotalAmount,
        decimal newTotalAmount,
        DateTime occurredOnUtc,
        string correlationId,
        string? causationId,
        string source)
        : base(occurredOnUtc, correlationId, causationId, source)
    {
        OrderId = orderId;
        PreviousTotalAmount = previousTotalAmount;
        NewTotalAmount = newTotalAmount;
    }

    public Guid OrderId { get; init; }
    public decimal PreviousTotalAmount { get; init; }
    public decimal NewTotalAmount { get; init; }
}
