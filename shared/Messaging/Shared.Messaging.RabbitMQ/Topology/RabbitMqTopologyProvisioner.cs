using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

using Shared.Messaging.RabbitMQ.Options;

namespace Shared.Messaging.RabbitMQ.Topology;

internal static class RabbitMqTopologyProvisioner
{
    internal static string ToExchangeTypeString(this RabbitMqExchangeType type) => type switch
    {
        RabbitMqExchangeType.Direct  => ExchangeType.Direct,
        RabbitMqExchangeType.Topic   => ExchangeType.Topic,
        RabbitMqExchangeType.Headers => ExchangeType.Headers,
        _                            => ExchangeType.Fanout
    };

    internal static async Task DeclareExchangeAsync(
        IChannel channel,
        string exchange,
        string type,
        bool durable,
        ILogger logger,
        CancellationToken ct = default)
    {
        logger.LogInformation("[topology] exchange declared: {Exchange} (type={Type}, durable={Durable})", exchange, type, durable);
        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: type,
            durable: durable,
            autoDelete: false,
            cancellationToken: ct);
    }

    internal static async Task DeclareConsumerTopologyAsync(
        IChannel channel,
        RabbitMqConsumerOptions options,
        ILogger logger,
        CancellationToken ct = default)
    {
        var exchangeType = options.ExchangeType.ToExchangeTypeString();

        logger.LogInformation("[topology] exchange declared: {Exchange} (type={Type}, durable={Durable})", options.Exchange, exchangeType, options.Durable);
        await channel.ExchangeDeclareAsync(
            exchange: options.Exchange,
            type: exchangeType,
            durable: options.Durable,
            autoDelete: false,
            cancellationToken: ct);

        var arguments = new Dictionary<string, object?>();
        if (options.EnableDeadLetterQueue)
        {
            arguments["x-dead-letter-exchange"]    = options.ResolvedDeadLetterExchange;
            arguments["x-dead-letter-routing-key"] = options.ResolvedDeadLetterRoutingKey;
        }

        logger.LogInformation("[topology] queue declared: {Queue} (durable={Durable}, exclusive={Exclusive}, autoDelete={AutoDelete})", options.Queue, options.Durable, options.Exclusive, options.AutoDelete);
        await channel.QueueDeclareAsync(
            queue: options.Queue,
            durable: options.Durable,
            exclusive: options.Exclusive,
            autoDelete: options.AutoDelete,
            arguments: arguments,
            cancellationToken: ct);

        logger.LogInformation("[topology] binding: {Queue} -> {Exchange} (routingKey={RoutingKey})", options.Queue, options.Exchange, options.RoutingKey);
        await channel.QueueBindAsync(
            queue: options.Queue,
            exchange: options.Exchange,
            routingKey: options.RoutingKey,
            cancellationToken: ct);

        if (!options.EnableDeadLetterQueue) return;

        logger.LogInformation("[topology] DLQ exchange declared: {DlqExchange} (type=direct, durable={Durable})", options.ResolvedDeadLetterExchange, options.Durable);
        await channel.ExchangeDeclareAsync(
            exchange: options.ResolvedDeadLetterExchange,
            type: ExchangeType.Direct,
            durable: options.Durable,
            autoDelete: false,
            cancellationToken: ct);

        logger.LogInformation("[topology] DLQ queue declared: {DlqQueue}", options.ResolvedDeadLetterQueue);
        await channel.QueueDeclareAsync(
            queue: options.ResolvedDeadLetterQueue,
            durable: options.Durable,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        logger.LogInformation("[topology] DLQ binding: {DlqQueue} -> {DlqExchange} (routingKey={RoutingKey})", options.ResolvedDeadLetterQueue, options.ResolvedDeadLetterExchange, options.ResolvedDeadLetterRoutingKey);
        await channel.QueueBindAsync(
            queue: options.ResolvedDeadLetterQueue,
            exchange: options.ResolvedDeadLetterExchange,
            routingKey: options.ResolvedDeadLetterRoutingKey,
            cancellationToken: ct);
    }
}
