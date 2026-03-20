using Orders.API.Infrastructure;
using Shared.Contracts.Events;
using Shared.Messaging;

namespace Orders.API.Application.Orders;

public class CreateOrderCommandHandler(OrdersDbContext dbContext, IMessageBus messageBus)
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
        await messageBus.PublishAsync(@event, "order-created");

        return order.Id;
    }
}
