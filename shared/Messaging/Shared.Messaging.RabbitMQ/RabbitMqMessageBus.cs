using RabbitMQ.Client;
using Shared.Messaging.Abstractions;
using Shared.Messaging.RabbitMQ.Connection;
using RmqExchangeType = RabbitMQ.Client.ExchangeType;
using System.Text;

namespace Shared.Messaging.RabbitMQ;

internal sealed class RabbitMqMessageBus(IRabbitMqConnectionFactory connectionFactory) : IMessageBus
{
    public async Task Publish(
        string message,
        string destination,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default
    )
    {
        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            Headers = headers?.ToDictionary(h => h.Key, h => (object?)h.Value),
        };

        await channel.ExchangeDeclareAsync(
            destination,
            RmqExchangeType.Fanout,
            cancellationToken: cancellationToken
        );

        var messageBody = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: destination,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: messageBody,
            cancellationToken: cancellationToken
        );
    }
}
