using Microsoft.EntityFrameworkCore;

using Orders.API.Application.Orders;

using Shared.Outbox.Abstractions;

using Shared.Outbox.Database;

namespace Orders.API.Infrastructure;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options), IOutboxDbContext
{
    public DbSet<Order> Orders { get; init; }
    public DbSet<OutboxMessage> OutboxMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfig("outbox_messages"));
    }
}