using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Notification.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedEvent>
{
    public async Task ConsumeAsync(OrderCreatedEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Notification] Order received: {OrderId}", message.OrderId);
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
