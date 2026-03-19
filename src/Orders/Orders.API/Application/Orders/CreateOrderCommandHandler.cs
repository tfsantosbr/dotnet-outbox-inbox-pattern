using Orders.API.Infrastructure;
using Orders.API.Infrastructure.Messaging;
using Shared.Contracts.Events;

namespace Orders.API.Application.Orders;

public class CreateOrderCommandHandler(OrdersDbContext dbContext, RabbitMqPublisher publisher)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command)
    {
        var order = new Order(
            Guid.NewGuid(),
            command.CustomerId,
            DateTime.UtcNow,
            command.TotalAmount);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var @event = new OrderCreatedEvent(order.Id, order.CustomerId, order.TotalAmount, order.CreatedOnUtc);
        await publisher.PublishAsync(@event, "order-created");

        return order.Id;
    }
}
