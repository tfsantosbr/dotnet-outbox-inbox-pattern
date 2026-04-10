using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

using Shared.Messaging.RabbitMQ.Options;

namespace Shared.Messaging.RabbitMQ.Connection;

internal sealed class PersistentRabbitMqConnection(
    IOptions<RabbitMqOptions> options,
    ILogger<PersistentRabbitMqConnection> logger) : IPersistentRabbitMqConnection
{
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsConnected => _connection?.IsOpen == true;

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) return;

            var factory = new ConnectionFactory { Uri = new Uri(options.Value.ConnectionString) };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            logger.LogInformation("RabbitMQ persistent connection established");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(CreateChannelOptions? channelOptions = null, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);

        var opts = options.Value;
        return await _connection!.CreateChannelAsync(
            channelOptions ?? new CreateChannelOptions(
                publisherConfirmationsEnabled: opts.PublisherConfirmationsEnabled,
                publisherConfirmationTrackingEnabled: opts.PublisherConfirmationTrackingEnabled),
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}