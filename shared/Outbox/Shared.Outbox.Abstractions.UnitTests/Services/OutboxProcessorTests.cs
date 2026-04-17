using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Outbox.Abstractions.Database;
using Shared.Outbox.Abstractions.Interfaces;
using Shared.Outbox.Abstractions.Models;
using Shared.Outbox.Abstractions.Services;
using Shared.Outbox.Abstractions.Settings;
using Polly;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Shared.Outbox.Abstractions.UnitTests.Services;

public class OutboxProcessorTests
{
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options), IOutboxDbContext
    {
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    }

    private const string ModuleName = "test-module";

    private static OutboxMessage CreateTestMessage(string destination = "test-destination") =>
        OutboxMessage.Create(destination, Guid.NewGuid(), new { Name = "Test" }, DateTime.UtcNow);

    private static OutboxBackgroundService<TestDbContext> CreateService(
        IMessageBus messageBus,
        IOutboxStorage outboxStorage,
        OutboxProcessorOptions? processorOptions = null)
    {
        var options = processorOptions ?? new OutboxProcessorOptions
        {
            IntervalInSeconds = 0,
            BatchSize = 10,
        };

        var services = new ServiceCollection();
        services.AddSingleton(messageBus);
        services.AddTransient<IOutboxStorage>(_ => outboxStorage);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = NullLogger<OutboxProcessor<TestDbContext>>.Instance;

        var processor = new OutboxProcessor<TestDbContext>(
            ModuleName,
            storageKey: null,
            scopeFactory,
            logger,
            ResiliencePipeline.Empty
        );

        return new OutboxBackgroundService<TestDbContext>(processor, Options.Create(options));
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
            .PublishAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageExists_ShouldPublishMessageAndMarkAsPublished()
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
            .PublishAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(),
                Arg.Any<CancellationToken>()
            );

        await outboxStorage
            .Received()
            .UpdateMessagesAsync(Arg.Any<IReadOnlyList<OutboxMessage>>(), Arg.Any<CancellationToken>());

        Assert.NotNull(message.PublishedOnUtc);
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
            .PublishAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(),
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
            .UpdateMessagesAsync(Arg.Any<IReadOnlyList<OutboxMessage>>(), Arg.Any<CancellationToken>());

        Assert.NotNull(message.PublishedOnUtc);
        Assert.NotNull(message.Error);
        Assert.Equal(exceptionMessage, message.Error);
    }
}