using Orders.API.Infrastructure;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

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
            newTotalAmount: order.TotalAmount,
            occurredOnUtc: DateTime.UtcNow,
            correlationId: correlationId,
            causationId: order.Id.ToString(),
            source: "Orders.API");

        var headers = new Dictionary<string, string> { { "X-Correlation-Id", correlationId } };
        await messageBus.PublishAsync(@event, headers);

        await dbContext.SaveChangesAsync();

        return true;
    }
}
