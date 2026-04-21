using Shared.Inbox.Abstractions.Consumers;
using Shared.Inbox.Abstractions.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;
using Shared.Messaging.RabbitMQ.Bus;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Consumers;
using Shared.Messaging.RabbitMQ.Options;
using Shared.Messaging.RabbitMQ.Topology;

namespace Shared.Messaging.RabbitMQ.Extensions;

public static class RabbitMqMessagingExtensions
{
    public static MessagingBuilder UseRabbitMq(
        this MessagingBuilder builder,
        Action<RabbitMqOptions> configure)
    {
        var options = new RabbitMqOptions();
        configure(options);
        options.Validate();

        builder.Services.Configure<RabbitMqOptions>(configure);
        builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        builder.Services.AddSingleton<IPersistentRabbitMqConnection, PersistentRabbitMqConnection>();
        builder.Services.AddSingleton<IPublishTopologyRegistry, PublishTopologyRegistry>();
        builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

        return builder;
    }

    public static MessagingBuilder AddPublishOptions<TMessage>(
        this MessagingBuilder builder,
        Action<RabbitMqPublishOptions> configure)
    {
        var options = new RabbitMqPublishOptions();
        configure(options);
        options.Validate();
        builder.Services.AddSingleton(new PublishTopologyEntry(typeof(TMessage), options));
        return builder;
    }

    public static MessagingBuilder AddConsumer<TConsumer, TMessage>(
        this MessagingBuilder builder,
        Action<RabbitMqConsumerOptions> configure)
        where TConsumer : class, IMessageConsumer<TMessage>
    {
        var options = new RabbitMqConsumerOptions();
        configure(options);
        options.Validate();

        builder.Services.AddSingleton(options);
        builder.Services.AddScoped<TConsumer>();
        builder.Services.AddHostedService(sp =>
            new RabbitMqConsumerWorker<TMessage, TConsumer>(
                sp.GetRequiredService<IRabbitMqConnectionFactory>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                options,
                sp.GetRequiredService<ILogger<RabbitMqConsumerWorker<TMessage, TConsumer>>>()));

        return builder;
    }

    public static MessagingBuilder AddInboxConsumer<TConsumer, TMessage, TContext>(
        this MessagingBuilder builder,
        Action<RabbitMqConsumerOptions> configure)
        where TConsumer : class, IMessageConsumer<TMessage>
        where TContext : DbContext
    {
        var options = new RabbitMqConsumerOptions();
        configure(options);
        options.Validate();

        builder.Services.AddSingleton(options);
        builder.Services.AddScoped<TConsumer>();
        builder.Services.AddScoped(sp =>
            new InboxConsumerDecorator<TMessage>(
                sp.GetRequiredService<TConsumer>(),
                sp.GetRequiredService<TContext>(),
                sp.GetRequiredService<IInboxStorage>(),
                options.ConsumerName,
                sp.GetRequiredService<ILogger<InboxConsumerDecorator<TMessage>>>()));

        builder.Services.AddHostedService(sp =>
            new RabbitMqConsumerWorker<TMessage, InboxConsumerDecorator<TMessage>>(
                sp.GetRequiredService<IRabbitMqConnectionFactory>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                options,
                sp.GetRequiredService<ILogger<RabbitMqConsumerWorker<TMessage, InboxConsumerDecorator<TMessage>>>>()));

        return builder;
    }

    public static async Task ApplyRabbitMqTopologyAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        var sp = host.Services;

        var connection      = sp.GetRequiredService<IPersistentRabbitMqConnection>();
        var publishEntries  = sp.GetRequiredService<IEnumerable<PublishTopologyEntry>>();
        var consumerOptions = sp.GetRequiredService<IEnumerable<RabbitMqConsumerOptions>>();
        var logger          = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RabbitMQ.Topology");

        await connection.EnsureConnectedAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await using (channel)
        {
            foreach (var entry in publishEntries)
            {
                if (entry.Options is not RabbitMqPublishOptions rmq) continue;
                var type = rmq.ExchangeType.ToExchangeTypeString();
                await RabbitMqTopologyProvisioner.DeclareExchangeAsync(
                    channel, rmq.Destination, type, rmq.Durable, logger, cancellationToken);
            }

            foreach (var opts in consumerOptions)
            {
                await RabbitMqTopologyProvisioner.DeclareConsumerTopologyAsync(
                    channel, opts, logger, cancellationToken);
            }
        }
    }
}
