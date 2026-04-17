using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shared.Outbox.Abstractions.Database;
using Shared.Outbox.Abstractions.Extensions;
using Shared.Outbox.Abstractions.Interfaces;
using Shared.Outbox.Abstractions.Models;
using Shared.Outbox.EntityFrameworkCore.PostgreSQL.Extensions;
using Shared.Messaging.Abstractions.Interfaces;

namespace Shared.Outbox.EntityFrameworkCore.PostgreSQL.UnitTests.Extensions;

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
                entity.Property(e => e.OccurredOnUtc).IsRequired();
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
    public void AddOutbox_ShouldRegisterOutboxPublisherAsScoped()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        services.AddOutbox<TestDbContext>()
            .UsePostgreSQLStorage<TestDbContext>(o => o.ConnectionString = "Host=localhost")
            .WithSettings(o => { o.IntervalInSeconds = 5; o.BatchSize = 10; });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IOutboxPublisher) &&
            d.ServiceKey is null);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddKeyedOutbox_ShouldRegisterOutboxPublisherAsKeyedScoped()
    {
        // Arrange
        var services = CreateBaseServices();
        const string moduleName = "orders";

        // Act
        services.AddKeyedOutbox<TestDbContext>(moduleName)
            .UsePostgreSQLStorage<TestDbContext>(o => o.ConnectionString = "Host=localhost")
            .WithSettings(o => { o.IntervalInSeconds = 5; o.BatchSize = 10; });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IOutboxPublisher) &&
            d.ServiceKey is string key && key == moduleName);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddOutbox_ShouldRegisterOutboxStorageAsTransient()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        services.AddOutbox<TestDbContext>()
            .UsePostgreSQLStorage<TestDbContext>(o => o.ConnectionString = "Host=localhost");

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IOutboxStorage) &&
            d.ServiceKey is null);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddKeyedOutbox_ShouldRegisterOutboxStorageAsKeyedTransient()
    {
        // Arrange
        var services = CreateBaseServices();
        const string moduleName = "orders";

        // Act
        services.AddKeyedOutbox<TestDbContext>(moduleName)
            .UsePostgreSQLStorage<TestDbContext>(o => o.ConnectionString = "Host=localhost");

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IOutboxStorage) &&
            d.ServiceKey is string key && key == moduleName);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddOutbox_ShouldRegisterHostedService()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        services.AddOutbox<TestDbContext>()
            .UsePostgreSQLStorage<TestDbContext>(o => o.ConnectionString = "Host=localhost");

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IHostedService));

        Assert.NotNull(descriptor);
    }

    [Fact]
    public void AddOutbox_ShouldReturnOutboxBuilder()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        var builder = services.AddOutbox<TestDbContext>();

        // Assert
        Assert.NotNull(builder);
        Assert.Same(services, builder.Services);
    }

    [Fact]
    public void AddOutbox_WhenProviderIsBuilt_ShouldResolveHostedService()
    {
        // Arrange
        var services = CreateBaseServices();
        services.AddOutbox<TestDbContext>()
            .UsePostgreSQLStorage<TestDbContext>(o => o.ConnectionString = "Host=localhost");

        // Act
        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();

        // Assert
        Assert.NotEmpty(hostedServices);
    }

    [Fact]
    public void AddKeyedOutbox_TwoModules_ShouldRegisterIndependentStorages()
    {
        // Arrange
        var services = CreateBaseServices();

        // Act
        services.AddKeyedOutbox<TestDbContext>("orders")
            .UsePostgreSQLStorage<TestDbContext>(o => { o.ConnectionString = "Host=localhost;Database=orders"; });
        services.AddKeyedOutbox<TestDbContext>("inventory")
            .UsePostgreSQLStorage<TestDbContext>(o => { o.ConnectionString = "Host=localhost;Database=inventory"; });

        // Assert
        var storageDescriptors = services
            .Where(d => d.ServiceType == typeof(IOutboxStorage))
            .ToList();

        Assert.Equal(2, storageDescriptors.Count);
        Assert.Contains(storageDescriptors, d => d.ServiceKey is "orders");
        Assert.Contains(storageDescriptors, d => d.ServiceKey is "inventory");
    }
}