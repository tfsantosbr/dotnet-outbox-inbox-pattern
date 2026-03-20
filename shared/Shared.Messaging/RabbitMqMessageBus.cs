using RabbitMQ.Client;
using System.Text.Json;

namespace Shared.Messaging;

public class RabbitMqMessageBus(IConnection connection) : IMessageBus
{
    public async Task PublishAsync<T>(T @event, string exchange, CancellationToken cancellationToken = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event);

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: string.Empty,
            body: body,
            cancellationToken: cancellationToken);
    }
}
