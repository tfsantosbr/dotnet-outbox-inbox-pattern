using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Notification.Consumer.Consumers;

public class OrderTotalAmountUpdatedConsumer(ILogger<OrderTotalAmountUpdatedConsumer> logger)
    : IMessageConsumer<OrderTotalAmountUpdatedIntegrationEvent>
{
    public async Task ConsumeAsync(OrderTotalAmountUpdatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue("occurred-on-utc", out var occurredOnUtc);
        context.Headers.TryGetValue("correlation-id", out var correlationId);
        context.Headers.TryGetValue("causation-id", out var causationId);
        context.Headers.TryGetValue("source", out var source);

        logger.LogInformation(
            "[Notification] Order total amount updated: {OrderId} | {PreviousTotalAmount} → {NewTotalAmount} | OccurredOnUtc: {OccurredOnUtc} CorrelationId: {CorrelationId} CausationId: {CausationId} Source: {Source}",
            message.OrderId, message.PreviousTotalAmount, message.NewTotalAmount,
            occurredOnUtc ?? "unknown", correlationId ?? "unknown", causationId ?? "unknown", source ?? "unknown");

        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
