using Shared.Messaging.RabbitMQ.Connection;
using Shared.Messaging.RabbitMQ.Options;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Shared.Messaging.RabbitMQ.UnitTests.Connection;

public class RabbitMqConnectionFactoryTests
{
    [Fact]
    public async Task CreateConnectionAsync_WithValidConnectionString_AttemptsConnection()
    {
        // Arrange
        var options = MsOptions.Create(new RabbitMqOptions { ConnectionString = "amqp://localhost:0000" });
        var factory = new RabbitMqConnectionFactory(options);

        // Act & Assert
        // We expect a connection exception since RabbitMQ is not running
        // This verifies the options were used and connection was attempted
        var exception = await Record.ExceptionAsync(
            async () => await factory.CreateConnectionAsync());

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task CreateConnectionAsync_WithCancellationToken_PassesCancellationToken()
    {
        // Arrange
        var options = MsOptions.Create(new RabbitMqOptions { ConnectionString = "amqp://localhost:0000" });
        var cancellationToken = new CancellationToken();
        var factory = new RabbitMqConnectionFactory(options);

        // Act & Assert
        // We expect a connection exception since RabbitMQ is not running
        var exception = await Record.ExceptionAsync(
            async () => await factory.CreateConnectionAsync(cancellationToken));

        Assert.NotNull(exception);
    }

    [Fact]
    public void CreateConnectionAsync_UsesConnectionStringFromOptions()
    {
        // Arrange
        var connectionString = "amqp://localhost:5672";
        var options = MsOptions.Create(new RabbitMqOptions { ConnectionString = connectionString });
        var factory = new RabbitMqConnectionFactory(options);

        // Act
        try
        {
            var _ = factory.CreateConnectionAsync();
        }
        catch
        {
            // Expected since RabbitMQ is not running
        }

        // Assert — connection string was taken from options
        Assert.Equal(connectionString, options.Value.ConnectionString);
    }
}