using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Options;

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

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: options.PrefetchCount, global: false, stoppingToken);

        var asyncConsumer = new AsyncEventingBasicConsumer(channel);
        asyncConsumer.ReceivedAsync += (_, ea) => ProcessMessageAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: options.Queue,
            autoAck: false,
            consumer: asyncConsumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    internal async Task ProcessMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        CancellationToken ct)
    {
        TMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<TMessage>(ea.Body.Span);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON payload from queue {Queue}; discarding", options.Queue);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (message is null)
        {
            logger.LogError("Null deserialization result from queue {Queue}; discarding", options.Queue);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            using var scope  = scopeFactory.CreateScope();
            var consumer     = scope.ServiceProvider.GetRequiredService<TConsumer>();
            var context      = new RabbitMqMessageContext(
                channel,
                ea.DeliveryTag,
                ea.BasicProperties.Headers,
                ea.BasicProperties.MessageId,
                ea.Redelivered);

            var result = await consumer.ConsumeAsync(message, context, ct);

            if (options.AckMode == AckMode.AutoOnSuccess)
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
            }
            else
            {
                if (result.IsAck)
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
                else
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: result.Requeue, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message from queue {Queue}", options.Queue);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }
}
