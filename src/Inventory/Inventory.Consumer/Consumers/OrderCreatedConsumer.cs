using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedEvent>
{
    public async Task ConsumeAsync(OrderCreatedEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Inventory] Order received: {OrderId}", message.OrderId);
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
