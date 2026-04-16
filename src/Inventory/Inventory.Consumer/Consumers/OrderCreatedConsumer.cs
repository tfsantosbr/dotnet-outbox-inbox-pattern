using Inventory.Consumer.Application.Products.Commands;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Inventory.Consumer.Consumers;

public class OrderCreatedConsumer(ReduceStockCommandHandler reduceStockCommandHandler)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public async Task<ConsumerResult> ConsumeAsync(
        OrderCreatedIntegrationEvent message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        var command = new ReduceStockCommand(message.ProductId, message.Quantity);
        await reduceStockCommandHandler.HandleAsync(command, cancellationToken);
        return ConsumerResult.Ack();
    }
}
