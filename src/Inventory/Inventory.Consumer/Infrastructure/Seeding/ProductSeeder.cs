using Inventory.Consumer.Application.FeatureFlags;
using Inventory.Consumer.Domain.Products;

using Microsoft.FeatureManagement;

namespace Inventory.Consumer.Infrastructure.Seeding;

public class ProductSeeder(
    InventoryDbContext dbContext,
    IFeatureManager featureManager,
    ILogger<ProductSeeder> logger) : IDatabaseSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await featureManager.IsEnabledAsync(FeatureFlags.ProductSeed))
        {
            logger.LogInformation("Feature flag '{FeatureFlag}' is disabled, skipping product seed",
                FeatureFlags.ProductSeed);
            return;
        }

        if (dbContext.Products.Any())
        {
            logger.LogInformation("Products already seeded, skipping");
            return;
        }

        logger.LogInformation("Seeding default product");

        dbContext.Products.Add(new Product(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "Default Product",
            stock: 10_000));

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product seed completed");
    }
}