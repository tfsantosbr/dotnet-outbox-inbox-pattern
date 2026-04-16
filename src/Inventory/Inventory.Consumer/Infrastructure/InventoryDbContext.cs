using Inbox.Abstractions.Database;
using Inbox.Abstractions.Models;

using Inventory.Consumer.Domain.Products;

using Microsoft.EntityFrameworkCore;

namespace Inventory.Consumer.Infrastructure;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IInboxDbContext
{
    public DbSet<Product> Products { get; init; }
    public DbSet<InboxMessage> InboxMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
    }
}