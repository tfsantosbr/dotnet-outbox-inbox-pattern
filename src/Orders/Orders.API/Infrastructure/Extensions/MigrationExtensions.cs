using Microsoft.EntityFrameworkCore;

namespace Orders.API.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        db.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS orders");
        db.Database.Migrate();
    }
}
