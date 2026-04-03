using Shared.Events;

namespace Shared.Contracts.Events;

public record OrderCustomerUpdatedIntegrationEvent : IntegrationEvent
{
    public OrderCustomerUpdatedIntegrationEvent(Guid orderId, Guid previousCustomerId, Guid newCustomerId)
    {
        OrderId = orderId;
        PreviousCustomerId = previousCustomerId;
        NewCustomerId = newCustomerId;
    }

    public Guid OrderId { get; init; }
    public Guid PreviousCustomerId { get; init; }
    public Guid NewCustomerId { get; init; }
}
