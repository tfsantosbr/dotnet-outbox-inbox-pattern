using Microsoft.Extensions.Options;

using RabbitMQ.Client;

using Shared.Messaging.RabbitMQ.Options;

namespace Shared.Messaging.RabbitMQ.Connection;

public sealed class RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options) : IRabbitMqConnectionFactory
{
    public async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(options.Value.ConnectionString),
        };

        return await factory.CreateConnectionAsync(cancellationToken);
    }
}