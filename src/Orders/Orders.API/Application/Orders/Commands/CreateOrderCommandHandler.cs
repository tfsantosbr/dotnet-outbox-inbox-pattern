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
            Guid.CreateVersion7(),
            command.CustomerId,
            DateTime.UtcNow,
            command.TotalAmount);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var @event = new OrderCreatedIntegrationEvent(
            orderId: order.Id,
            customerId: order.CustomerId,
            totalAmount: order.TotalAmount,
            occurredOnUtc: order.CreatedOnUtc,
            correlationId: correlationId,
            causationId: order.Id.ToString(),
            source: "Orders.API"
            );

        var headers = new Dictionary<string, string> { { "X-Correlation-Id", correlationId } };
        await messageBus.Publish(JsonSerializer.Serialize(@event), "order-created", headers);

        return order.Id;
    }
}
