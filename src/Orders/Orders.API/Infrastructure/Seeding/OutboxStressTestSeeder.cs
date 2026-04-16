using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Orders.API.Application.FeatureFlags;

namespace Orders.API.Infrastructure.Seeding;

public class OutboxStressTestSeeder(
    OrdersDbContext dbContext,
    IFeatureManager featureManager,
    ILogger<OutboxStressTestSeeder> logger) : IDatabaseSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await featureManager.IsEnabledAsync(FeatureFlags.OutboxStressTestSeed))
        {
            logger.LogInformation("Feature flag '{FeatureFlag}' is disabled, skipping outbox stress test seed",
                FeatureFlags.OutboxStressTestSeed);
            return;
        }

        if (dbContext.OutboxMessages.Any())
        {
            logger.LogInformation("Outbox messages already seeded, skipping");
            return;
        }

        const int totalMessages = 3_000_000;
        const int batchSize = 1_000_000;
        int totalBatches = totalMessages / batchSize;

        logger.LogInformation("Seeding {TotalMessages} outbox messages in {TotalBatches} batches of {BatchSize}",
            totalMessages, totalBatches, batchSize);

        for (int batch = 0; batch < totalBatches; batch++)
        {
            int offset = batch * batchSize;
            logger.LogInformation("Inserting batch {Batch}/{TotalBatches} (rows {From} to {To})",
                batch + 1, totalBatches, offset + 1, offset + batchSize);

            dbContext.Database.SetCommandTimeout(300);
            dbContext.Database.ExecuteSql($"""
                INSERT INTO "orders"."outbox_messages"
                    ("Id", "Type", "Destination", "Content", "Headers", "OccurredOnUtc")
                SELECT
                    gen_random_uuid(),
                    'Shared.Contracts.Events.OrderCreatedIntegrationEvent, Shared.Contracts',
                    'order-created',
                    json_build_object(
                        'OrderId',     gen_random_uuid(),
                        'CustomerId',  gen_random_uuid(),
                        'TotalAmount', 100.00,
                        'ProductId',   '00000000-0000-0000-0000-000000000001',
                        'Quantity',    1
                    )::jsonb,
                    json_build_object('correlation-id', gen_random_uuid()::text)::jsonb,
                    NOW() + (({offset} + gs) * interval '1 millisecond')
                FROM generate_series(1, {batchSize}) AS gs
                """);

            logger.LogInformation("Batch {Batch}/{TotalBatches} inserted successfully", batch + 1, totalBatches);
        }

        logger.LogInformation("Seed completed: {TotalMessages} outbox messages inserted", totalMessages);
    }
}
