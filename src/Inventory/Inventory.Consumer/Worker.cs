using Inventory.Consumer.Consumers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using System.Text.Json;

namespace Inventory.Consumer;

public class Worker(IConnection connection, OrderCreatedConsumer consumer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: "order-created",
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "inventory.order-created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: "inventory.order-created",
            exchange: "order-created",
            routingKey: string.Empty,
            cancellationToken: stoppingToken);

        var asyncConsumer = new AsyncEventingBasicConsumer(channel);
        asyncConsumer.ReceivedAsync += async (_, ea) =>
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(ea.Body.Span);
            if (@event is not null)
                consumer.Consume(@event);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        };

        await channel.BasicConsumeAsync(
            queue: "inventory.order-created",
            autoAck: false,
            consumer: asyncConsumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
