using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Messaging.Abstractions;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;
using Shared.Outbox.Extensions;

namespace Shared.Outbox.Tests.Extensions;

public class OutboxExtensionsTests
{
    private sealed class TestDbContext : DbContext, IOutboxDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

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
            });
        }
    }

    private static IServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IMessageBus>());
        services.AddDbContext<TestDbContext>(opts =>
            opts.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services;
    }

    [Fact]
    public void AddOutboxServices_ShouldRegisterOutboxPublisherAsKeyedScoped()
    {
        // Arrange
        var services = CreateBaseServices();
        const string moduleName = "orders";

        // Act
        services.AddOutboxServices<TestDbContext>(moduleName, "Host=localhost", 5, 10);

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IOutboxPublisher) &&
            d.ServiceKey is string key && key == moduleName);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddOutboxServices_ShouldRegisterHostedService()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        services.AddOutboxServices<TestDbContext>("orders", "Host=localhost", 5, 10);

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IHostedService));

        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddOutboxServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        var result = services.AddOutboxServices<TestDbContext>("orders", "Host=localhost", 5, 10);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddOutboxServices_WhenProviderIsBuilt_ShouldResolveHostedService()
    {
        // Arrange
        var services = CreateBaseServices();
        services.AddOutboxServices<TestDbContext>("orders", "Host=localhost", 5, 10);

        // Act
        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        // Assert
        Assert.NotEmpty(hostedServices);
    }
}
