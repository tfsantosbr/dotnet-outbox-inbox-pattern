using RabbitMQ.Client;
using System.Text.Json;

namespace Orders.API.Infrastructure.Messaging;

public class RabbitMqPublisher(IConnection connection)
{
    public async Task PublishAsync<T>(T @event, string exchange)
    {
        await using var channel = await connection.CreateChannelAsync();

        var body = JsonSerializer.SerializeToUtf8Bytes(@event);

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: string.Empty,
            body: body);
    }
}
