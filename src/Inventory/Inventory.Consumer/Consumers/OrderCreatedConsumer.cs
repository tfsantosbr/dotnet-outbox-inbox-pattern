using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedEvent>
{
    public async Task ConsumeAsync(OrderCreatedEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue("X-Correlation-Id", out var correlationId);
        logger.LogInformation("[Inventory] Order received: {OrderId} CorrelationId: {CorrelationId}", message.OrderId, correlationId ?? "unknown");
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
