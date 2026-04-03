using Orders.API.Infrastructure;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;
using static Shared.Messaging.Abstractions.MessageHeaders;

namespace Orders.API.Application.Orders.Commands;

public class UpdateOrderTotalAmountCommandHandler(
    OrdersDbContext dbContext,
    IMessageBus messageBus)
{
    public async Task<bool> HandleAsync(UpdateOrderTotalAmountCommand command, string correlationId)
    {
        var order = await dbContext.Orders.FindAsync(command.OrderId);

        if (order is null)
            return false;

        var previousTotalAmount = order.TotalAmount;

        order.UpdateTotalAmount(command.TotalAmount);

        var @event = new OrderTotalAmountUpdatedIntegrationEvent(
            orderId: order.Id,
            previousTotalAmount: previousTotalAmount,
            newTotalAmount: order.TotalAmount);

        var headers = new Dictionary<string, string>
        {
            { CorrelationId, correlationId },
            { CausationId, order.Id.ToString() },
            { Source, "orders-api" }
        };
        
        await messageBus.PublishAsync(@event, headers);

        await dbContext.SaveChangesAsync();

        return true;
    }
}
