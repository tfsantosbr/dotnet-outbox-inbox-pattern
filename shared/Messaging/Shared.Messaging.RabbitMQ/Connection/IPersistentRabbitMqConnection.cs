using RabbitMQ.Client;

namespace Shared.Messaging.RabbitMQ.Connection;

public interface IPersistentRabbitMqConnection : IAsyncDisposable
{
    bool IsConnected { get; }
    Task EnsureConnectedAsync(CancellationToken cancellationToken = default);
    Task<IChannel> CreateChannelAsync(CreateChannelOptions? channelOptions = null, CancellationToken cancellationToken = default);
}