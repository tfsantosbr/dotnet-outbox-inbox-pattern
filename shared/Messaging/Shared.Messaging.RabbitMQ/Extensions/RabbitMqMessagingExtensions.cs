using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shared.Messaging.Abstractions;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Consumers;
using Shared.Messaging.RabbitMQ.Options;

namespace Shared.Messaging.RabbitMQ.Extensions;

public static class RabbitMqMessagingExtensions
{
    public static MessagingBuilder UseRabbitMq(
        this MessagingBuilder builder,
        Action<RabbitMqOptions> configure)
    {
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

        builder.Services.AddScoped<TConsumer>();
        builder.Services.AddHostedService(sp =>
            new RabbitMqConsumerWorker<TMessage, TConsumer>(
                sp.GetRequiredService<IRabbitMqConnectionFactory>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                options,
                sp.GetRequiredService<ILogger<RabbitMqConsumerWorker<TMessage, TConsumer>>>()));

        return builder;
    }
}