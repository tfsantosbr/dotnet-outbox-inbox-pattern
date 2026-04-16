using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Inventory.Consumer.Consumers;

public class OrderTotalAmountUpdatedConsumer(ILogger<OrderTotalAmountUpdatedConsumer> logger)
    : IMessageConsumer<OrderTotalAmountUpdatedIntegrationEvent>
{
    public Task<ConsumerResult> ConsumeAsync(OrderTotalAmountUpdatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue(MessageHeaders.OccurredOnUtc, out var occurredOnUtc);
        context.Headers.TryGetValue(MessageHeaders.CorrelationId, out var correlationId);
        context.Headers.TryGetValue(MessageHeaders.CausationId, out var causationId);
        context.Headers.TryGetValue(MessageHeaders.Source, out var source);

        logger.LogInformation(
            "[Inventory] Order total amount updated: {OrderId} | {PreviousTotalAmount} → {NewTotalAmount} | OccurredOnUtc: {OccurredOnUtc} CorrelationId: {CorrelationId} CausationId: {CausationId} Source: {Source}",
            message.OrderId, message.PreviousTotalAmount, message.NewTotalAmount,
            occurredOnUtc ?? "unknown", correlationId ?? "unknown", causationId ?? "unknown", source ?? "unknown");

        return Task.FromResult(ConsumerResult.Ack());
    }
}