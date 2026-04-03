using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Inventory.Consumer.Consumers;

public class OrderCustomerUpdatedConsumer(ILogger<OrderCustomerUpdatedConsumer> logger)
    : IMessageConsumer<OrderCustomerUpdatedIntegrationEvent>
{
    public async Task ConsumeAsync(OrderCustomerUpdatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue("X-Correlation-Id", out var correlationId);
        logger.LogInformation(
            "[Inventory] Order customer updated: {OrderId} | {PreviousCustomerId} → {NewCustomerId} CorrelationId: {CorrelationId}",
            message.OrderId, message.PreviousCustomerId, message.NewCustomerId, correlationId ?? "unknown");
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
