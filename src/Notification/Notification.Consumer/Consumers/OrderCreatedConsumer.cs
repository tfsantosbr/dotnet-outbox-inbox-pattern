using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Notification.Consumer.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public Task<ConsumerResult> ConsumeAsync(OrderCreatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue(MessageHeaders.OccurredOnUtc, out var occurredOnUtc);
        context.Headers.TryGetValue(MessageHeaders.CorrelationId, out var correlationId);
        context.Headers.TryGetValue(MessageHeaders.CausationId, out var causationId);
        context.Headers.TryGetValue(MessageHeaders.Source, out var source);

        logger.LogInformation(
            "[Notification] Order received: {OrderId} | OccurredOnUtc: {OccurredOnUtc} CorrelationId: {CorrelationId} CausationId: {CausationId} Source: {Source}",
            message.OrderId, occurredOnUtc ?? "unknown", correlationId ?? "unknown", causationId ?? "unknown", source ?? "unknown");

        return Task.FromResult(ConsumerResult.Ack());
    }
}