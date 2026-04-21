using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shared.Outbox.Abstractions.Database;
using Shared.Outbox.Abstractions.Interfaces;
using Shared.Outbox.Abstractions.Models;
using Shared.Outbox.EntityFrameworkCore.PostgreSQL.Publisher;
using Shared.Events;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Shared.Outbox.EntityFrameworkCore.PostgreSQL.UnitTests.Publisher;

public class OutboxPublisherTests
{
    private record TestIntegrationEvent : IntegrationEvent { }

    private sealed class TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options) : DbContext(options), IOutboxDbContext
    {
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Destination).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.OccurredOnUtc).IsRequired();
                entity.Property(e => e.Headers);
                entity.Property(e => e.ProcessedOnUtc);
                entity.Property(e => e.ErrorHandledOnUtc);
                entity.Property(e => e.Error);
            });
        }
    }

    private static TestOutboxDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestOutboxDbContext(options);
    }

    private static IPublishTopologyRegistry CreateRegistry(string destination)
    {
        var registry = Substitute.For<IPublishTopologyRegistry>();
        registry.GetOptions(typeof(TestIntegrationEvent))
            .Returns(new PublishOptions { Destination = destination });
        return registry;
    }

    [Fact]
    public async Task PublishAsync_WhenCalled_ShouldAddOutboxMessageToContext()
    {
        // Arrange
        const string expectedDestination = "test-destination";
        await using var context = CreateInMemoryContext();
        var registry = CreateRegistry(expectedDestination);
        var publisher = new OutboxPublisher<TestOutboxDbContext>(context, registry);
        var integrationEvent = new TestIntegrationEvent { MessageId = Guid.NewGuid(), OccurredOnUtc = DateTime.UtcNow };

        // Act
        await publisher.PublishAsync(integrationEvent);
        await context.SaveChangesAsync();

        // Assert
        var messages = context.OutboxMessages.ToList();
        Assert.Single(messages);
        Assert.Equal(integrationEvent.MessageId, messages[0].Id);
        Assert.Equal(expectedDestination, messages[0].Destination);
    }

    [Fact]
    public async Task PublishAsync_WhenCalledWithHeaders_ShouldAddMessageWithHeaders()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = CreateRegistry("test-destination");
        var publisher = new OutboxPublisher<TestOutboxDbContext>(context, registry);
        var integrationEvent = new TestIntegrationEvent { MessageId = Guid.NewGuid(), OccurredOnUtc = DateTime.UtcNow };
        var headers = new Dictionary<string, string>
        {
            { "correlation-id", "abc-123" },
            { "tenant-id", "tenant-1" },
        };

        // Act
        await publisher.PublishAsync(integrationEvent, headers);
        await context.SaveChangesAsync();

        // Assert
        var messages = context.OutboxMessages.ToList();
        Assert.Single(messages);
        var storedHeaders = messages[0].GetHeaders();
        Assert.NotNull(storedHeaders);
        Assert.Equal("abc-123", storedHeaders["correlation-id"]);
        Assert.Equal("tenant-1", storedHeaders["tenant-id"]);
    }

    [Fact]
    public async Task PublishAsync_WhenCalledWithNullHeaders_ShouldAddMessageWithNullHeaders()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = CreateRegistry("test-destination");
        var publisher = new OutboxPublisher<TestOutboxDbContext>(context, registry);
        var integrationEvent = new TestIntegrationEvent { MessageId = Guid.NewGuid(), OccurredOnUtc = DateTime.UtcNow };

        // Act
        await publisher.PublishAsync(integrationEvent, null);
        await context.SaveChangesAsync();

        // Assert
        var messages = context.OutboxMessages.ToList();
        Assert.Single(messages);
        Assert.Null(messages[0].Headers);
    }

    [Fact]
    public async Task PublishAsync_WhenNoOptionsRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var registry = Substitute.For<IPublishTopologyRegistry>();
        registry.GetOptions(typeof(TestIntegrationEvent)).Returns((PublishOptions?)null);
        var publisher = new OutboxPublisher<TestOutboxDbContext>(context, registry);
        var integrationEvent = new TestIntegrationEvent { MessageId = Guid.NewGuid(), OccurredOnUtc = DateTime.UtcNow };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(integrationEvent));
    }
}