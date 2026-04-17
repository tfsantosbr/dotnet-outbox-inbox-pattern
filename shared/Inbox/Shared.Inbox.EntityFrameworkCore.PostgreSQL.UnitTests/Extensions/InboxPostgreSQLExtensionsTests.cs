using FluentAssertions;

using Shared.Inbox.Abstractions.Database;
using Shared.Inbox.Abstractions.Extensions;
using Shared.Inbox.Abstractions.Interfaces;
using Shared.Inbox.Abstractions.Models;
using Shared.Inbox.EntityFrameworkCore.PostgreSQL.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Inbox.EntityFrameworkCore.PostgreSQL.UnitTests.Extensions;

public class InboxPostgreSQLExtensionsTests
{
    [Fact]
    public void UsePostgreSQLStorage_RegistersIInboxStorageAsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<FakeDbContext>(o => o.UseInMemoryDatabase("test"));

        services.AddInbox().UsePostgreSQLStorage<FakeDbContext>();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IInboxStorage));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void UsePostgreSQLStorage_ReturnsInboxBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<FakeDbContext>(o => o.UseInMemoryDatabase("test"));

        var builder = services.AddInbox();
        var result = builder.UsePostgreSQLStorage<FakeDbContext>();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void UsePostgreSQLStorage_WithOptions_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<FakeDbContext>(o => o.UseInMemoryDatabase("test"));

        services.AddInbox().UsePostgreSQLStorage<FakeDbContext>(opt =>
        {
            opt.Schema = "myschema";
            opt.TableName = "my_inbox";
        });

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IInboxStorage));
        descriptor.Should().NotBeNull();
    }

    private sealed class FakeDbContext(DbContextOptions<FakeDbContext> options)
        : DbContext(options), IInboxDbContext
    {
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
    }
}