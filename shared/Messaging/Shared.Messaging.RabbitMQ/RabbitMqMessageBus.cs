using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Messaging.Abstractions;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Options;
using RmqExchangeType = RabbitMQ.Client.ExchangeType;

namespace Shared.Messaging.RabbitMQ;

internal sealed class RabbitMqMessageBus(
    IPersistentRabbitMqConnection connection,
    IPublishTopologyRegistry topologyRegistry,
    ILogger<RabbitMqMessageBus> logger) : IMessageBus, IAsyncDisposable
{
    private IChannel? _channel;
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    public async Task PublishAsync<TMessage>(
        TMessage message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var options = topologyRegistry.GetOptions(typeof(TMessage))
            ?? throw new InvalidOperationException(
                $"No PublishOptions registered for '{typeof(TMessage).Name}'. " +
                $"Call AddPublishOptions<{typeof(TMessage).Name}>() during configuration.");

        var json = JsonSerializer.Serialize(message);
        await PublishCoreAsync(json, options, headers, cancellationToken);
    }

    public async Task PublishAsync(
        string message,
        string destination,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var options = new PublishOptions { Destination = destination };
        await PublishCoreAsync(message, options, headers, cancellationToken);
    }

    private async Task PublishCoreAsync(
        string json,
        PublishOptions baseOptions,
        IDictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        var rmqOptions = baseOptions as RabbitMqPublishOptions;

        var exchangeType = rmqOptions?.ExchangeType switch
        {
            RabbitMqExchangeType.Direct  => RmqExchangeType.Direct,
            RabbitMqExchangeType.Topic   => RmqExchangeType.Topic,
            RabbitMqExchangeType.Headers => RmqExchangeType.Headers,
            _                            => RmqExchangeType.Fanout
        };

        var durable    = rmqOptions?.Durable ?? false;
        var routingKey = rmqOptions?.RoutingKey ?? string.Empty;

        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetOrCreateChannelAsync(cancellationToken);

            var properties = new BasicProperties
            {
                Headers = headers?.ToDictionary(h => h.Key, h => (object?)h.Value),
            };

            await channel.ExchangeDeclareAsync(
                exchange: baseOptions.Destination,
                type: exchangeType,
                durable: durable,
                cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: baseOptions.Destination,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task<IChannel> GetOrCreateChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) return _channel;

        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        logger.LogDebug("RabbitMQ publisher channel created");

        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
    }
}
