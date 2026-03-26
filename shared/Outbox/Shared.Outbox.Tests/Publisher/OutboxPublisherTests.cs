using Microsoft.EntityFrameworkCore;

using Shared.Events;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;
using Shared.Outbox.Publisher;

namespace Shared.Outbox.Tests.Publisher;

public class OutboxPublisherTests
{
    private record TestIntegrationEvent : IntegrationEvent
    {
        public TestIntegrationEvent() : base(default, string.Empty, null, string.Empty) { }
    }

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
                entity.Property(e => e.OccurredOn).IsRequired();
                entity.Property(e => e.Headers);
                entity.Property(e => e.ProcessedOn);
                entity.Property(e => e.ErrorHandledOn);
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

    [Fact]
    public async Task Publish_WhenCalled_ShouldAddOutboxMessageToContext()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var publisher = new OutboxPublisher<TestOutboxDbContext>(context);
        var integrationEvent = new TestIntegrationEvent { Id = Guid.NewGuid(), OccurredOnUtc = DateTime.UtcNow };

        // Act
        await publisher.Publish(integrationEvent, "test-destination");
        await context.SaveChangesAsync();

        // Assert
        var messages = context.OutboxMessages.ToList();
        Assert.Single(messages);
        Assert.Equal(integrationEvent.Id, messages[0].Id);
        Assert.Equal("test-destination", messages[0].Destination);
    }

    [Fact]
    public async Task Publish_WhenCalledWithHeaders_ShouldAddMessageWithHeaders()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var publisher = new OutboxPublisher<TestOutboxDbContext>(context);
        var integrationEvent = new TestIntegrationEvent { Id = Guid.NewGuid(), OccurredOnUtc = DateTime.UtcNow };
        var headers = new Dictionary<string, string>
        {
            { "correlation-id", "abc-123" },
            { "tenant-id", "tenant-1" },
        };

        // Act
        await publisher.Publish(integrationEvent, "test-destination", headers);
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
    public async Task Publish_WhenCalledWithNullHeaders_ShouldAddMessageWithNullHeaders()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var publisher = new OutboxPublisher<TestOutboxDbContext>(context);
        var integrationEvent = new TestIntegrationEvent { Id = Guid.NewGuid(), OccurredOnUtc = DateTime.UtcNow };

        // Act
        await publisher.Publish(integrationEvent, "test-destination", null);
        await context.SaveChangesAsync();

        // Assert
        var messages = context.OutboxMessages.ToList();
        Assert.Single(messages);
        Assert.Null(messages[0].Headers);
    }
}
