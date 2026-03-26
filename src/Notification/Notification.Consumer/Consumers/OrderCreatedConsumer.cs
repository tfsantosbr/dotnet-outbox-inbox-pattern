using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Notification.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public async Task ConsumeAsync(OrderCreatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue("X-Correlation-Id", out var correlationId);
        logger.LogInformation("[Notification] Order received: {OrderId} CorrelationId: {CorrelationId}", message.OrderId, correlationId ?? "unknown");
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
