using Orders.API.Infrastructure;
using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Interfaces;
using static Shared.Messaging.Abstractions.Models.MessageHeaders;

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