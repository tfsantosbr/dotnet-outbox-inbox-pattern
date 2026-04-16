using Inventory.Consumer.Application.Products.Commands;
using Inventory.Consumer.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(
    ReduceStockCommandHandler reduceStockCommandHandler,
    InventoryDbContext dbContext,
    ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public async Task<ConsumerResult> ConsumeAsync(
        OrderCreatedIntegrationEvent message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var productExists = await dbContext.Products
            .AnyAsync(p => p.Id == message.ProductId, cancellationToken);

        if (!productExists)
        {
            logger.LogWarning(
                "Product {ProductId} not found. Sending message to dead-letter queue. OrderId: {OrderId}",
                message.ProductId,
                message.OrderId);

            return ConsumerResult.Nack(requeue: false);
        }

        var command = new ReduceStockCommand(message.ProductId, message.Quantity);
        await reduceStockCommandHandler.HandleAsync(command, cancellationToken);
        return ConsumerResult.Ack();
    }
}