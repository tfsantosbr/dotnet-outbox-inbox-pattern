using Orders.API.Infrastructure.Seeding;

namespace Orders.API.Infrastructure.Extensions;

public static class SeedingExtensions
{
    public static IServiceCollection AddDatabaseSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDatabaseSeeder
    {
        services.AddScoped<IDatabaseSeeder, TSeeder>();
        return services;
    }

    public static async Task RunSeedersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<IDatabaseSeeder>();

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync();
        }
    }
}