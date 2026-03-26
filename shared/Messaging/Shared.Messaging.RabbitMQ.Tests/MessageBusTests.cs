using NSubstitute;
using RabbitMQ.Client;
using Shared.Messaging.RabbitMQ.Connection;

namespace Shared.Messaging.RabbitMQ.Tests;

public class RabbitMqMessageBusTests
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly RabbitMqMessageBus _messageBus;

    public RabbitMqMessageBusTests()
    {
        _connectionFactory = Substitute.For<IRabbitMqConnectionFactory>();
        _connection = Substitute.For<IConnection>();
        _channel = Substitute.For<IChannel>();
        _messageBus = new RabbitMqMessageBus(_connectionFactory);

        _connectionFactory.CreateConnectionAsync(Arg.Any<CancellationToken>()).Returns(_connection);
        _connection
            .CreateChannelAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_channel);
    }

    [Fact]
    public async Task Publish_WithoutHeaders_PublishesMessageSuccessfully()
    {
        // Arrange
        var message = "test message";
        var queueName = "test-queue";

        // Act
        await _messageBus.Publish(message, queueName);

        // Assert
        await _connectionFactory.Received(1).CreateConnectionAsync(Arg.Any<CancellationToken>());
        await _connection
            .Received(1)
            .CreateChannelAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _channel
            .Received(1)
            .ExchangeDeclareAsync(
                queueName,
                ExchangeType.Fanout,
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await _channel
            .Received(1)
            .BasicPublishAsync(
                exchange: queueName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: Arg.Any<BasicProperties>(),
                body: Arg.Any<ReadOnlyMemory<byte>>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Publish_WithHeaders_PublishesMessageWithHeaders()
    {
        // Arrange
        var message = "test message";
        var queueName = "test-queue";
        var correlationId = "test-correlation-id";
        var headers = new Dictionary<string, string>
        {
            { "X-Correlation-ID", correlationId },
            { "Custom-Header", "custom-value" },
        };

        // Act
        await _messageBus.Publish(message, queueName, headers);

        // Assert
        await _connectionFactory.Received(1).CreateConnectionAsync(Arg.Any<CancellationToken>());
        await _connection
            .Received(1)
            .CreateChannelAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _channel
            .Received(1)
            .ExchangeDeclareAsync(
                queueName,
                ExchangeType.Fanout,
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await _channel
            .Received(1)
            .BasicPublishAsync(
                exchange: queueName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: Arg.Is<BasicProperties>(props =>
                    props.Headers != null
                    && props.Headers.ContainsKey("X-Correlation-ID")
                    && props.Headers["X-Correlation-ID"]!.ToString() == correlationId
                ),
                body: Arg.Any<ReadOnlyMemory<byte>>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Publish_WithCancellationToken_PassesCancellationTokenToAllOperations()
    {
        // Arrange
        var message = "test message";
        var queueName = "test-queue";
        var cancellationToken = new CancellationToken();

        // Act
        await _messageBus.Publish(message, queueName, cancellationToken: cancellationToken);

        // Assert
        await _connectionFactory.Received(1).CreateConnectionAsync(cancellationToken);
        await _connection.Received(1).CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel
            .Received(1)
            .ExchangeDeclareAsync(
                queueName,
                ExchangeType.Fanout,
                cancellationToken: cancellationToken
            );
        await _channel
            .Received(1)
            .BasicPublishAsync(
                exchange: queueName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: Arg.Any<BasicProperties>(),
                body: Arg.Any<ReadOnlyMemory<byte>>(),
                cancellationToken: cancellationToken
            );
    }

    [Fact]
    public async Task Publish_ValidMessage_CallsBasicPublishAsync()
    {
        // Arrange
        var message = "test message with special chars";
        var queueName = "test-queue";

        // Act
        await _messageBus.Publish(message, queueName);

        // Assert
        await _channel
            .Received(1)
            .BasicPublishAsync(
                exchange: queueName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: Arg.Any<BasicProperties>(),
                body: Arg.Any<ReadOnlyMemory<byte>>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Publish_WithNullHeaders_PublishesMessageSuccessfully()
    {
        // Arrange
        var message = "test message";
        var queueName = "test-queue";

        // Act
        await _messageBus.Publish(message, queueName, headers: null);

        // Assert
        await _connectionFactory.Received(1).CreateConnectionAsync(Arg.Any<CancellationToken>());
        await _connection
            .Received(1)
            .CreateChannelAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _channel
            .Received(1)
            .ExchangeDeclareAsync(
                queueName,
                ExchangeType.Fanout,
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await _channel
            .Received(1)
            .BasicPublishAsync(
                exchange: queueName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: Arg.Is<BasicProperties>(props => props.Headers == null),
                body: Arg.Any<ReadOnlyMemory<byte>>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Publish_WithEmptyHeaders_PublishesMessageSuccessfully()
    {
        // Arrange
        var message = "test message";
        var queueName = "test-queue";
        var headers = new Dictionary<string, string>();

        // Act
        await _messageBus.Publish(message, queueName, headers);

        // Assert
        await _connectionFactory.Received(1).CreateConnectionAsync(Arg.Any<CancellationToken>());
        await _connection
            .Received(1)
            .CreateChannelAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _channel
            .Received(1)
            .ExchangeDeclareAsync(
                queueName,
                ExchangeType.Fanout,
                cancellationToken: Arg.Any<CancellationToken>()
            );
        await _channel
            .Received(1)
            .BasicPublishAsync(
                exchange: queueName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: Arg.Is<BasicProperties>(props =>
                    props.Headers != null && props.Headers.Count == 0
                ),
                body: Arg.Any<ReadOnlyMemory<byte>>(),
                cancellationToken: Arg.Any<CancellationToken>()
            );
    }
}
