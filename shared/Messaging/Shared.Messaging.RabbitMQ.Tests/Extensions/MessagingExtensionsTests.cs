using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.Abstractions;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Extensions;
using Shared.Messaging.RabbitMQ.Options;

namespace Shared.Messaging.RabbitMQ.Tests.Extensions;

public class RabbitMqMessagingExtensionsTests
{
    [Fact]
    public void UseRabbitMq_ShouldRegisterMessageBusAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessaging().UseRabbitMq(o => o.ConnectionString = "amqp://localhost");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageBus));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void UseRabbitMq_ShouldRegisterConnectionFactoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessaging().UseRabbitMq(o => o.ConnectionString = "amqp://localhost");

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRabbitMqConnectionFactory)
        );
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void UseRabbitMq_ShouldReturnMessagingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddMessaging();

        // Act
        var result = builder.UseRabbitMq(o => o.ConnectionString = "amqp://localhost");

        // Assert
        Assert.Same(builder, result);
    }
}
