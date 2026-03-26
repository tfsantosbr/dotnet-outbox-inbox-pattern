using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging.Abstractions;
using Shared.Messaging.RabbitMQ.Connection;
using RmqExchangeType = RabbitMQ.Client.ExchangeType;
using System.Text.Json;

namespace Shared.Messaging.RabbitMQ.Consumers;

internal sealed class RabbitMqConsumerWorker<TMessage, TConsumer>(
    IRabbitMqConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    ConsumerOptions options,
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
            Abstractions.ExchangeType.Fanout => RmqExchangeType.Fanout,
            Abstractions.ExchangeType.Direct => RmqExchangeType.Direct,
            Abstractions.ExchangeType.Topic => RmqExchangeType.Topic,
            Abstractions.ExchangeType.Headers => RmqExchangeType.Headers,
            _ => RmqExchangeType.Fanout
        };

        await channel.ExchangeDeclareAsync(
            exchange: options.Exchange,
            type: exchangeType,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: options.Queue,
            durable: options.Durable,
            exclusive: options.Exclusive,
            autoDelete: options.AutoDelete,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: options.Queue,
            exchange: options.Exchange,
            routingKey: options.RoutingKey,
            cancellationToken: stoppingToken);

        var asyncConsumer = new AsyncEventingBasicConsumer(channel);
        asyncConsumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(ea.Body.Span);
                if (message is not null)
                {
                    using var scope = scopeFactory.CreateScope();
                    var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
                    var context = new RabbitMqMessageContext(channel, ea.DeliveryTag, ea.BasicProperties.Headers);
                    await consumer.ConsumeAsync(message, context, stoppingToken);
                }
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
