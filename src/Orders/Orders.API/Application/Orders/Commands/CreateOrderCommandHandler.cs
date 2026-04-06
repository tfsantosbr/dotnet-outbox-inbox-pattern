using Orders.API.Infrastructure;
using Shared.Contracts.Events;
using Shared.Outbox.Abstractions;
using static Shared.Messaging.Abstractions.MessageHeaders;

namespace Orders.API.Application.Orders.Commands;

public class CreateOrderCommandHandler(
    OrdersDbContext dbContext,
    IOutboxPublisher outboxPublisher)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, string correlationId)
    {
        var order = new Order(
            Guid.CreateVersion7(),
            command.CustomerId,
            DateTime.UtcNow,
            command.TotalAmount);

        dbContext.Orders.Add(order);

        var @event = new OrderCreatedIntegrationEvent(
            orderId: order.Id,
            customerId: order.CustomerId,
            totalAmount: order.TotalAmount);

        var headers = new Dictionary<string, string>
        {
            { CorrelationId, correlationId },
            { CausationId, order.Id.ToString() },
            { Source, "orders-api" }
        };

        await outboxPublisher.PublishAsync(@event, headers);

        await dbContext.SaveChangesAsync();

        return order.Id;
    }
}