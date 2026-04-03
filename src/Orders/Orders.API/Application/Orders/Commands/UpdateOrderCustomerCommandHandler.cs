using Microsoft.EntityFrameworkCore;

using Orders.API.Infrastructure;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;

namespace Orders.API.Application.Orders.Commands;

public class UpdateOrderCustomerCommandHandler(
    OrdersDbContext dbContext,
    IMessageBus messageBus)
{
    public async Task<bool> HandleAsync(UpdateOrderCustomerCommand command, string correlationId)
    {
        var order = await dbContext.Orders.FindAsync(command.OrderId);

        if (order is null)
            return false;

        var previousCustomerId = order.CustomerId;

        order.UpdateCustomer(command.CustomerId);

        var @event = new OrderCustomerUpdatedIntegrationEvent(
            orderId: order.Id,
            previousCustomerId: previousCustomerId,
            newCustomerId: order.CustomerId,
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
