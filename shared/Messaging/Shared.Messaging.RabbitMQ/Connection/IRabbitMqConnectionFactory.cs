using RabbitMQ.Client;

namespace Shared.Messaging.RabbitMQ.Connection;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}