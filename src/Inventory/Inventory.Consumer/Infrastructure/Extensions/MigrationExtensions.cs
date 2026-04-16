using Microsoft.EntityFrameworkCore;

namespace Inventory.Consumer.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS inventory");
        db.Database.Migrate();
    }
}
