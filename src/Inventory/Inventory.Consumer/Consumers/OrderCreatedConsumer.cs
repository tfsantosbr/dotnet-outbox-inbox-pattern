using InboxPattern.Abstractions.Interfaces;
using InboxPattern.Abstractions.Models;

using Inventory.Consumer.Application.Products.Commands;
using Inventory.Consumer.Infrastructure;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(
    ILogger<OrderCreatedConsumer> logger,
    InventoryDbContext dbContext,
    IInboxStorage inboxStorage,
    ReduceStockCommandHandler reduceStockCommandHandler)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    private const string ConsumerName = "inventory.order-created";

    public async Task ConsumeAsync(
        OrderCreatedIntegrationEvent message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var messageId = context.MessageId;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            logger.LogWarning("[Inventory] Mensagem sem MessageId. Descartando sem requeue.");
            await context.NackAsync(requeue: false, cancellationToken: cancellationToken);
            return;
        }

        context.Headers.TryGetValue(MessageHeaders.OccurredOnUtc, out var occurredOnUtc);
        context.Headers.TryGetValue(MessageHeaders.CorrelationId, out var correlationId);
        context.Headers.TryGetValue(MessageHeaders.CausationId, out var causationId);
        context.Headers.TryGetValue(MessageHeaders.Source, out var source);

        logger.LogInformation(
            "[Inventory] Order received: {OrderId} | ProductId: {ProductId} | Quantity: {Quantity} | OccurredOnUtc: {OccurredOnUtc} CorrelationId: {CorrelationId} CausationId: {CausationId} Source: {Source}",
            message.OrderId, message.ProductId, message.Quantity,
            occurredOnUtc ?? "unknown", correlationId ?? "unknown",
            causationId ?? "unknown", source ?? "unknown");

        await using var efTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var inboxMessage = InboxMessage.Create(messageId, ConsumerName, DateTime.UtcNow);
        var inboxResult = await inboxStorage.TryRegisterAsync(inboxMessage, cancellationToken);

        if (inboxResult.IsDuplicate)
        {
            await efTransaction.CommitAsync(cancellationToken);
            await context.AckAsync(cancellationToken: cancellationToken);
            return;
        }

        var command = new ReduceStockCommand(message.ProductId, message.Quantity);
        await reduceStockCommandHandler.HandleAsync(command, cancellationToken);

        await efTransaction.CommitAsync(cancellationToken);
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
