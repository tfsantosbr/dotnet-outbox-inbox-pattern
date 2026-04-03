using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Notification.Consumer.Consumers;

public class OrderCustomerUpdatedConsumer(ILogger<OrderCustomerUpdatedConsumer> logger)
    : IMessageConsumer<OrderCustomerUpdatedIntegrationEvent>
{
    public async Task ConsumeAsync(OrderCustomerUpdatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue("occurred-on-utc", out var occurredOnUtc);
        context.Headers.TryGetValue("correlation-id", out var correlationId);
        context.Headers.TryGetValue("causation-id", out var causationId);
        context.Headers.TryGetValue("source", out var source);

        logger.LogInformation(
            "[Notification] Order customer updated: {OrderId} | {PreviousCustomerId} → {NewCustomerId} | OccurredOnUtc: {OccurredOnUtc} CorrelationId: {CorrelationId} CausationId: {CausationId} Source: {Source}",
            message.OrderId, message.PreviousCustomerId, message.NewCustomerId,
            occurredOnUtc ?? "unknown", correlationId ?? "unknown", causationId ?? "unknown", source ?? "unknown");

        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
