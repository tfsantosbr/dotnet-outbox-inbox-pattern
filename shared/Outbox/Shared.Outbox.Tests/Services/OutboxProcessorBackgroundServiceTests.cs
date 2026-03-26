using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Shared.Messaging.Abstractions;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Services;
using Shared.Outbox.Settings;
using Shared.Outbox.Storage;

namespace Shared.Outbox.Tests.Services;

public class OutboxProcessorBackgroundServiceTests
{
    private static OutboxMessage CreateTestMessage(string destination = "test-destination")
    {
        return OutboxMessage.Create(destination, Guid.NewGuid(), new { Name = "Test" }, DateTime.UtcNow);
    }

    private static OutboxProcessorBackgroundService CreateService(
        IMessageBus messageBus,
        IOutboxStorage outboxStorage,
        OutboxSettings? settings = null)
    {
        var logger = Substitute.For<ILogger<OutboxProcessorBackgroundService>>();
        var resolvedSettings = settings ?? new OutboxSettings
        {
            IntervalInSeconds = 0,
            MessagesBatchSize = 10,
        };

        return new OutboxProcessorBackgroundService(
            "test-module",
            messageBus,
            logger,
            outboxStorage,
            ResiliencePipeline.Empty,
            resolvedSettings
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationTokenPreCancelled_ShouldExitWithoutProcessing()
    {
        // Arrange
        var messageBus = Substitute.For<IMessageBus>();
        var outboxStorage = Substitute.For<IOutboxStorage>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var service = CreateService(messageBus, outboxStorage);

        // Act & Assert - should complete without entering the while loop
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        await outboxStorage
            .DidNotReceive()
            .GetMessagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoMessages_ShouldNotCallMessageBusPublish()
    {
        // Arrange
        var messageBus = Substitute.For<IMessageBus>();
        var outboxStorage = Substitute.For<IOutboxStorage>();

        outboxStorage
            .GetMessagesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var service = CreateService(messageBus, outboxStorage);

        // Act
        await service.StartAsync(CancellationToken.None);
        try { await Task.Delay(200, cts.Token); } catch (OperationCanceledException) { }
        await service.StopAsync(CancellationToken.None);

        // Assert
        await messageBus
            .DidNotReceive()
            .Publish(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>?>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageExists_ShouldPublishMessageAndMarkAsProcessed()
    {
        // Arrange
        var messageBus = Substitute.For<IMessageBus>();
        var outboxStorage = Substitute.For<IOutboxStorage>();
        var message = CreateTestMessage("orders-exchange");

        outboxStorage
            .GetMessagesAsync(Arg.Any<CancellationToken>())
            .Returns(
                new List<OutboxMessage> { message },
                new List<OutboxMessage>()
            );

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var service = CreateService(messageBus, outboxStorage);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await messageBus
            .Received(1)
            .Publish(
                message.Content,
                message.Destination,
                Arg.Any<IDictionary<string, string>?>(),
                Arg.Any<CancellationToken>()
            );

        await outboxStorage
            .Received()
            .UpdateMessageAsync(message, Arg.Any<CancellationToken>());

        Assert.NotNull(message.ProcessedOn);
        Assert.Null(message.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublishThrows_ShouldMarkMessageWithError()
    {
        // Arrange
        var messageBus = Substitute.For<IMessageBus>();
        var outboxStorage = Substitute.For<IOutboxStorage>();
        var message = CreateTestMessage();
        var exceptionMessage = "Connection refused";

        messageBus
            .Publish(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>?>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new InvalidOperationException(exceptionMessage));

        outboxStorage
            .GetMessagesAsync(Arg.Any<CancellationToken>())
            .Returns(
                new List<OutboxMessage> { message },
                new List<OutboxMessage>()
            );

        var service = CreateService(messageBus, outboxStorage);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await outboxStorage
            .Received()
            .UpdateMessageAsync(message, Arg.Any<CancellationToken>());

        Assert.NotNull(message.ProcessedOn);
        Assert.NotNull(message.Error);
        Assert.Equal(exceptionMessage, message.Error);
    }
}
