using Shared.Contracts.Events;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
{
    public void Consume(OrderCreatedEvent @event)
    {
        logger.LogInformation("[Inventory] Order received: {OrderId}", @event.OrderId);
    }
}
