using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Options;

using RmqExchangeType = RabbitMQ.Client.ExchangeType;

namespace Shared.Messaging.RabbitMQ.Consumers;

internal sealed class RabbitMqConsumerWorker<TMessage, TConsumer>(
    IRabbitMqConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    RabbitMqConsumerOptions options,
    ILogger<RabbitMqConsumerWorker<TMessage, TConsumer>> logger)
    : BackgroundService
    where TConsumer : class, IMessageConsumer<TMessage>
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        var exchangeType = options.ExchangeType switch
        {
            RabbitMqExchangeType.Direct => RmqExchangeType.Direct,
            RabbitMqExchangeType.Topic => RmqExchangeType.Topic,
            RabbitMqExchangeType.Headers => RmqExchangeType.Headers,
            _ => RmqExchangeType.Fanout
        };

        if (options.EnableDeadLetterQueue)
        {
            await channel.ExchangeDeclareAsync(
                exchange: options.ResolvedDeadLetterExchange,
                type: RmqExchangeType.Fanout,
                durable: true,
                cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: options.ResolvedDeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await channel.QueueBindAsync(
                queue: options.ResolvedDeadLetterQueue,
                exchange: options.ResolvedDeadLetterExchange,
                routingKey: options.ResolvedDeadLetterRoutingKey,
                cancellationToken: stoppingToken);
        }

        await channel.ExchangeDeclareAsync(
            exchange: options.Exchange,
            type: exchangeType,
            cancellationToken: stoppingToken);

        var queueArguments = new Dictionary<string, object?>();
        if (options.EnableDeadLetterQueue)
        {
            queueArguments["x-dead-letter-exchange"] = options.ResolvedDeadLetterExchange;
            queueArguments["x-dead-letter-routing-key"] = options.ResolvedDeadLetterRoutingKey;
        }

        await channel.QueueDeclareAsync(
            queue: options.Queue,
            durable: options.Durable,
            exclusive: options.Exclusive,
            autoDelete: options.AutoDelete,
            arguments: queueArguments,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: options.Queue,
            exchange: options.Exchange,
            routingKey: options.RoutingKey,
            cancellationToken: stoppingToken);

        var asyncConsumer = new AsyncEventingBasicConsumer(channel);
        asyncConsumer.ReceivedAsync += async (_, ea) =>
        {
            var message = JsonSerializer.Deserialize<TMessage>(ea.Body.Span);
            if (message is null) return;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
                var context = new RabbitMqMessageContext(
                    channel,
                    ea.DeliveryTag,
                    ea.BasicProperties.Headers,
                    ea.BasicProperties.MessageId,
                    ea.Redelivered);

                var result = await consumer.ConsumeAsync(message, context, stoppingToken);

                if (result.IsAck)
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
                else
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: result.Requeue, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message from queue {Queue}", options.Queue);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: options.Queue,
            autoAck: false,
            consumer: asyncConsumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}