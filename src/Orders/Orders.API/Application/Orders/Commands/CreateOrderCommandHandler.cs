using Orders.API.Infrastructure;
using Shared.Contracts.Events;
using Shared.Messaging.Abstractions;
using System.Text.Json;

namespace Orders.API.Application.Orders.Commands;

public class CreateOrderCommandHandler(OrdersDbContext dbContext, IMessageBus messageBus)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, string correlationId)
    {
        var order = new Order(
            Guid.NewGuid(),
            command.CustomerId,
            DateTime.UtcNow,
            command.TotalAmount);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var @event = new OrderCreatedEvent(order.Id, order.CustomerId, order.TotalAmount, order.CreatedOnUtc);
        var headers = new Dictionary<string, string> { { "X-Correlation-Id", correlationId } };
        await messageBus.Publish(JsonSerializer.Serialize(@event), "order-created", headers);

        return order.Id;
    }
}
