using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Notification.Consumer.Consumers;

public class OrderTotalAmountUpdatedConsumer(ILogger<OrderTotalAmountUpdatedConsumer> logger)
    : IMessageConsumer<OrderTotalAmountUpdatedIntegrationEvent>
{
    public async Task ConsumeAsync(OrderTotalAmountUpdatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue("X-Correlation-Id", out var correlationId);
        logger.LogInformation(
            "[Notification] Order total amount updated: {OrderId} | {PreviousTotalAmount} → {NewTotalAmount} CorrelationId: {CorrelationId}",
            message.OrderId, message.PreviousTotalAmount, message.NewTotalAmount, correlationId ?? "unknown");
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
