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
            newCustomerId: order.CustomerId);

        var occurredOnUtc = DateTime.UtcNow;
        var headers = new Dictionary<string, string>
        {
            { "occurred-on-utc", occurredOnUtc.ToString("O") },
            { "correlation-id", correlationId },
            { "causation-id", order.Id.ToString() },
            { "source", "Orders.API" }
        };
        await messageBus.PublishAsync(@event, headers);

        await dbContext.SaveChangesAsync();

        return true;
    }
}
