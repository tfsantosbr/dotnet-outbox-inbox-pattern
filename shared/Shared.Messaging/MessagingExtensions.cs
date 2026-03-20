using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Shared.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessagingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnection>(sp =>
        {
            var config = configuration.GetSection("RabbitMQ");
            var factory = new ConnectionFactory
            {
                HostName = config["Host"] ?? "localhost",
                UserName = config["Username"] ?? "guest",
                Password = config["Password"] ?? "guest"
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

        return services;
    }
}
