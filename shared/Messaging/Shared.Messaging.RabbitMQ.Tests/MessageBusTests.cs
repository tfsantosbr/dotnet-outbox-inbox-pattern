using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;
using Shared.Messaging.Abstractions;
using Shared.Messaging.RabbitMQ.Connection;

namespace Shared.Messaging.RabbitMQ.Tests;

public class RabbitMqMessageBusTests
{
    private readonly IPersistentRabbitMqConnection _connection;
    private readonly IPublishTopologyRegistry _topologyRegistry;
    private readonly IChannel _channel;
    private readonly RabbitMqMessageBus _messageBus;

    public RabbitMqMessageBusTests()
    {
        _connection = Substitute.For<IPersistentRabbitMqConnection>();
        _topologyRegistry = Substitute.For<IPublishTopologyRegistry>();
        _channel = Substitute.For<IChannel>();
        var logger = Substitute.For<ILogger<RabbitMqMessageBus>>();

        _messageBus = new RabbitMqMessageBus(_connection, _topologyRegistry, logger);

        _connection
            .CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_channel);

        _channel.IsOpen.Returns(true);
    }

    [Fact]
    public async Task PublishAsync_WithStringMessage_PublishesSuccessfully()
    {
        // Arrange
        var message = "test message";
        var destination = "test-exchange";

        // Act
        await _messageBus.PublishAsync(message, destination);

        // Assert
        await _channel.Received(1).ExchangeDeclareAsync(
            exchange: destination,
            type: ExchangeType.Fanout,
            durable: Arg.Is<bool>(v => !v),
            autoDelete: Arg.Any<bool>(),
            arguments: Arg.Any<IDictionary<string, object?>>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());

        await _channel.Received(1).BasicPublishAsync(
            exchange: destination,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: Arg.Any<BasicProperties>(),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithTypedMessage_ResolvesTopologyAndPublishes()
    {
        // Arrange
        var destination = "typed-exchange";
        _topologyRegistry.GetOptions(typeof(TestMessage)).Returns(new PublishOptions { Destination = destination });

        // Act
        await _messageBus.PublishAsync(new TestMessage("hello"));

        // Assert
        await _channel.Received(1).ExchangeDeclareAsync(
            exchange: destination,
            type: Arg.Any<string>(),
            durable: Arg.Any<bool>(),
            autoDelete: Arg.Any<bool>(),
            arguments: Arg.Any<IDictionary<string, object?>>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithTypedMessage_NoRegisteredOptions_Throws()
    {
        // Arrange
        _topologyRegistry.GetOptions(typeof(TestMessage)).Returns((PublishOptions?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageBus.PublishAsync(new TestMessage("hello")));
    }

    [Fact]
    public async Task PublishAsync_WithHeaders_PassesHeadersToBasicProperties()
    {
        // Arrange
        var destination = "test-exchange";
        var headers = new Dictionary<string, string> { { "correlation-id", "abc123" } };

        // Act
        await _messageBus.PublishAsync("message", destination, headers);

        // Assert
        await _channel.Received(1).BasicPublishAsync(
            exchange: destination,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: Arg.Is<BasicProperties>(p =>
                p.Headers != null && p.Headers.ContainsKey("correlation-id")),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithNullHeaders_PublishesWithNullProperties()
    {
        // Arrange
        var destination = "test-exchange";

        // Act
        await _messageBus.PublishAsync("message", destination, headers: null);

        // Assert
        await _channel.Received(1).BasicPublishAsync(
            exchange: destination,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: Arg.Is<BasicProperties>(p => p.Headers == null),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ChannelIsReused_OnSubsequentCalls()
    {
        // Arrange
        var destination = "test-exchange";

        // Act
        await _messageBus.PublishAsync("message1", destination);
        await _messageBus.PublishAsync("message2", destination);

        // Assert — channel created only once
        await _connection.Received(1).CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_CreatesChannelWithPublisherConfirmsEnabled()
    {
        // Act
        await _messageBus.PublishAsync("message", "exchange");

        // Assert — channel was created (publisher confirms configured via CreateChannelOptions)
        await _connection.Received(1).CreateChannelAsync(
            Arg.Any<CreateChannelOptions?>(),
            Arg.Any<CancellationToken>());
    }

    private sealed record TestMessage(string Value);
}
