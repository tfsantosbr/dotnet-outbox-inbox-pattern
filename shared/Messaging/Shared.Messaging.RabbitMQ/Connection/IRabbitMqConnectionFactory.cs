using RabbitMQ.Client;

namespace Shared.Messaging.RabbitMQ.Connection;

internal interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
