using Shared.Events;

namespace Shared.Contracts.Events;

public record OrderCreatedIntegrationEvent : IntegrationEvent
{
    public OrderCreatedIntegrationEvent(Guid orderId, Guid customerId, decimal totalAmount, Guid productId, int quantity)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}