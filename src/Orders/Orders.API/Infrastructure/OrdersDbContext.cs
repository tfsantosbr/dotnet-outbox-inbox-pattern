using Microsoft.EntityFrameworkCore;

using Orders.API.Application.Orders;

using Outbox.Abstractions.Database;
using Outbox.Abstractions.Models;

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