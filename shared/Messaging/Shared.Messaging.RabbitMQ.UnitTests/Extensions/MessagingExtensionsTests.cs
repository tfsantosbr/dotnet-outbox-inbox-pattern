using Microsoft.Extensions.DependencyInjection;

using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Extensions;

namespace Shared.Messaging.RabbitMQ.UnitTests.Extensions;

public class RabbitMqMessagingExtensionsTests
{
    [Fact]
    public void UseRabbitMq_ShouldRegisterMessageBusAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessaging().UseRabbitMq(o => o.ConnectionString = "amqp://localhost");

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageBus));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
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
            d.ServiceType == typeof(IRabbitMqConnectionFactory));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void UseRabbitMq_ShouldRegisterPersistentConnectionAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessaging().UseRabbitMq(o => o.ConnectionString = "amqp://localhost");

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPersistentRabbitMqConnection));
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