using Inventory.Consumer.Infrastructure.Seeding;

namespace Inventory.Consumer.Infrastructure.Extensions;

public static class SeedingExtensions
{
    public static IServiceCollection AddDatabaseSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDatabaseSeeder
    {
        services.AddScoped<IDatabaseSeeder, TSeeder>();
        return services;
    }

    public static async Task RunSeedersAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<IDatabaseSeeder>();

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync();
        }
    }
}
