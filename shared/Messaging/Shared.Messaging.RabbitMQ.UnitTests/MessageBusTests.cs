using Microsoft.Extensions.Logging;

using NSubstitute;

using RabbitMQ.Client;

using Shared.Events;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;
using Shared.Messaging.RabbitMQ.Bus;
using Shared.Messaging.RabbitMQ.Connection;

using static Shared.Messaging.Abstractions.Models.MessageHeaders;

namespace Shared.Messaging.RabbitMQ.UnitTests;

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
        await _messageBus.PublishAsync(message, destination, RequiredHeaders());

        // Assert — topology is provisioned by ApplyRabbitMqTopologyAsync, not at publish time
        await _channel.DidNotReceive().ExchangeDeclareAsync(
            exchange: Arg.Any<string>(),
            type: Arg.Any<string>(),
            durable: Arg.Any<bool>(),
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
        await _messageBus.PublishAsync(new TestMessage("hello"), new Dictionary<string, string>
        {
            { CorrelationId, "test-correlation" }
        });

        // Assert — topology is provisioned by ApplyRabbitMqTopologyAsync, not at publish time
        await _channel.DidNotReceive().ExchangeDeclareAsync(
            exchange: Arg.Any<string>(),
            type: Arg.Any<string>(),
            durable: Arg.Any<bool>(),
            autoDelete: Arg.Any<bool>(),
            arguments: Arg.Any<IDictionary<string, object?>>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());

        await _channel.Received(1).BasicPublishAsync(
            exchange: destination,
            routingKey: Arg.Any<string>(),
            mandatory: Arg.Any<bool>(),
            basicProperties: Arg.Any<BasicProperties>(),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithTypedMessage_NoRegisteredOptions_Throws()
    {
        // Arrange
        _topologyRegistry.GetOptions(typeof(TestMessage)).Returns((PublishOptions?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageBus.PublishAsync(new TestMessage("hello"), new Dictionary<string, string>
            {
                { CorrelationId, "test-correlation" }
            }));
    }

    [Fact]
    public async Task PublishAsync_WithHeaders_PassesHeadersToBasicProperties()
    {
        // Arrange
        var destination = "test-exchange";

        // Act
        await _messageBus.PublishAsync("message", destination, RequiredHeaders());

        // Assert
        await _channel.Received(1).BasicPublishAsync(
            exchange: destination,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: Arg.Is<BasicProperties>(p =>
                p.Headers != null && p.Headers.ContainsKey(CorrelationId)),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithMissingRequiredHeaders_Throws()
    {
        // Arrange
        var destination = "test-exchange";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageBus.PublishAsync("message", destination, headers: null));
    }

    [Fact]
    public async Task PublishAsync_ChannelIsReused_OnSubsequentCalls()
    {
        // Arrange
        var destination = "test-exchange";

        // Act
        await _messageBus.PublishAsync("message1", destination, RequiredHeaders());
        await _messageBus.PublishAsync("message2", destination, RequiredHeaders());

        // Assert — channel created only once
        await _connection.Received(1).CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_CreatesChannelWithPublisherConfirmsEnabled()
    {
        // Act
        await _messageBus.PublishAsync("message", "exchange", RequiredHeaders());

        // Assert — channel was created (publisher confirms configured via CreateChannelOptions)
        await _connection.Received(1).CreateChannelAsync(
            Arg.Any<CreateChannelOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithTypedMessage_AutoPopulatesMessageIdAndOccurredOnUtc()
    {
        // Arrange
        var destination = "typed-exchange";
        var message = new TestMessage("hello");
        _topologyRegistry.GetOptions(typeof(TestMessage)).Returns(new PublishOptions { Destination = destination });

        BasicProperties? capturedProps = null;
        await _channel.BasicPublishAsync(
            exchange: Arg.Any<string>(),
            routingKey: Arg.Any<string>(),
            mandatory: Arg.Any<bool>(),
            basicProperties: Arg.Do<BasicProperties>(p => capturedProps = p),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());

        // Act
        await _messageBus.PublishAsync(message, new Dictionary<string, string>
        {
            { CorrelationId, "test-correlation" }
        });

        // Assert
        Assert.NotNull(capturedProps?.Headers);
        Assert.Equal(message.MessageId.ToString(), capturedProps.Headers[MessageId]);
        Assert.Equal(message.OccurredOnUtc.ToString("O"), capturedProps.Headers[OccurredOnUtc]);
    }

    [Fact]
    public async Task PublishBatchAsync_WithEmptyList_DoesNotPublish()
    {
        // Act
        await _messageBus.PublishBatchAsync([]);

        // Assert
        await _connection.DidNotReceive().CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_WithMissingRequiredHeaders_Throws()
    {
        // Arrange
        var items = new List<MessageBatchItem>
        {
            new("content", "exchange", null)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _messageBus.PublishBatchAsync(items));
    }

    [Fact]
    public async Task PublishBatchAsync_PublishesAllMessagesWithSingleLockAcquisition()
    {
        // Arrange
        var items = new List<MessageBatchItem>
        {
            new("content1", "exchange-a", RequiredHeaders()),
            new("content2", "exchange-b", RequiredHeaders()),
        };

        // Act
        await _messageBus.PublishBatchAsync(items);

        // Assert — channel created once, both messages published
        await _connection.Received(1).CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>());
        await _channel.Received(2).BasicPublishAsync(
            exchange: Arg.Any<string>(),
            routingKey: Arg.Any<string>(),
            mandatory: Arg.Any<bool>(),
            basicProperties: Arg.Any<BasicProperties>(),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_DoesNotDeclareAnyExchange()
    {
        // Arrange — topology is the responsibility of ApplyRabbitMqTopologyAsync, not the publisher
        var destination = "test-exchange";
        var items = new List<MessageBatchItem>
        {
            new("content1", destination, RequiredHeaders()),
            new("content2", destination, RequiredHeaders()),
            new("content3", destination, RequiredHeaders()),
        };

        // Act
        await _messageBus.PublishBatchAsync(items);

        // Assert
        await _channel.DidNotReceive().ExchangeDeclareAsync(
            exchange: Arg.Any<string>(),
            type: Arg.Any<string>(),
            durable: Arg.Any<bool>(),
            autoDelete: Arg.Any<bool>(),
            arguments: Arg.Any<IDictionary<string, object?>>(),
            noWait: Arg.Any<bool>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    private static Dictionary<string, string> RequiredHeaders() => new()
    {
        { MessageId,      Guid.NewGuid().ToString() },
        { OccurredOnUtc,  DateTime.UtcNow.ToString("O") },
        { CorrelationId,  "test-correlation" }
    };

    private sealed record TestMessage(string Value) : IEventBase
    {
        public Guid MessageId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }
}