using Shared.Contracts.Events;

namespace Notification.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
{
    public void Consume(OrderCreatedEvent @event)
    {
        logger.LogInformation("[Notification] Order received: {OrderId}", @event.OrderId);
    }
}
