using Inventory.Consumer.Application.Products.Commands;
using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(
    ILogger<OrderCreatedConsumer> logger,
    ReduceStockCommandHandler reduceStockCommandHandler)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public async Task ConsumeAsync(OrderCreatedIntegrationEvent message, IMessageContext context, CancellationToken cancellationToken = default)
    {
        context.Headers.TryGetValue(MessageHeaders.OccurredOnUtc, out var occurredOnUtc);
        context.Headers.TryGetValue(MessageHeaders.CorrelationId, out var correlationId);
        context.Headers.TryGetValue(MessageHeaders.CausationId, out var causationId);
        context.Headers.TryGetValue(MessageHeaders.Source, out var source);

        logger.LogInformation(
            "[Inventory] Order received: {OrderId} | ProductId: {ProductId} | Quantity: {Quantity} | OccurredOnUtc: {OccurredOnUtc} CorrelationId: {CorrelationId} CausationId: {CausationId} Source: {Source}",
            message.OrderId, message.ProductId, message.Quantity, occurredOnUtc ?? "unknown", correlationId ?? "unknown", causationId ?? "unknown", source ?? "unknown");

        var command = new ReduceStockCommand(message.ProductId, message.Quantity);
        await reduceStockCommandHandler.HandleAsync(command, cancellationToken);

        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
