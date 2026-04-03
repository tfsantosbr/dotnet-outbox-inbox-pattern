using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public async Task ConsumeAsync(OrderCreatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue("occurred-on-utc", out var occurredOnUtc);
        context.Headers.TryGetValue("correlation-id", out var correlationId);
        context.Headers.TryGetValue("causation-id", out var causationId);
        context.Headers.TryGetValue("source", out var source);

        logger.LogInformation(
            "[Inventory] Order received: {OrderId} | OccurredOnUtc: {OccurredOnUtc} CorrelationId: {CorrelationId} CausationId: {CausationId} Source: {Source}",
            message.OrderId, occurredOnUtc ?? "unknown", correlationId ?? "unknown", causationId ?? "unknown", source ?? "unknown");

        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
