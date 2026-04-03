using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

using Shared.Events;
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
        where TMessage : IEventBase
    {
        var options = GetPublishOptions<TMessage>();
        var json = JsonSerializer.Serialize(message);

        var mergedHeaders = headers is not null
            ? new Dictionary<string, string>(headers)
            : [];

        mergedHeaders[MessageHeaders.MessageId] = message.MessageId.ToString();
        mergedHeaders[MessageHeaders.OccurredOnUtc] = message.OccurredOnUtc.ToString("O");

        await PublishCoreAsync(json, options, mergedHeaders, cancellationToken);
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

    private PublishOptions GetPublishOptions<TMessage>() =>
        topologyRegistry.GetOptions(typeof(TMessage))
            ?? throw new InvalidOperationException(
                $"No PublishOptions registered for '{typeof(TMessage).Name}'. " +
                $"Call AddPublishOptions<{typeof(TMessage).Name}>() during configuration.");

    private static void ValidateRequiredHeaders(IDictionary<string, string>? headers)
    {
        string[] required = [MessageHeaders.MessageId, MessageHeaders.OccurredOnUtc, MessageHeaders.CorrelationId];
        var missing = required
            .Where(k => headers is null || !headers.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v))
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing required message headers: {string.Join(", ", missing)}");
    }

    private async Task PublishCoreAsync(
        string json,
        PublishOptions baseOptions,
        IDictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        ValidateRequiredHeaders(headers);
        var rmqOptions = baseOptions as RabbitMqPublishOptions;

        var exchangeType = rmqOptions?.ExchangeType switch
        {
            RabbitMqExchangeType.Direct => RmqExchangeType.Direct,
            RabbitMqExchangeType.Topic => RmqExchangeType.Topic,
            RabbitMqExchangeType.Headers => RmqExchangeType.Headers,
            _ => RmqExchangeType.Fanout
        };

        var durable = rmqOptions?.Durable ?? false;
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