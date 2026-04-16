using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Orders.API.Application.FeatureFlags;

namespace Orders.API.Infrastructure.Seeding;

/// <summary>
/// Seeds 10 outbox messages to test the Inbox Pattern deduplication logic.
/// The duplicate inbox entries are seeded separately by the InboxDuplicateTestSeeder
/// in Inventory.Consumer.
///
/// Expected result after the outbox processor runs:
///   - 6 messages processed normally by the inventory consumer
///   - 4 messages detected as duplicates and discarded (stock NOT reduced twice)
/// </summary>
public class InboxDuplicateTestSeeder(
    OrdersDbContext dbContext,
    IFeatureManager featureManager,
    ILogger<InboxDuplicateTestSeeder> logger) : IDatabaseSeeder
{
    // Deterministic GUIDs so the seed is idempotent
    private static readonly Guid[] MessageIds =
    [
        new("aaaaaaaa-0001-0000-0000-000000000000"),
        new("aaaaaaaa-0002-0000-0000-000000000000"),
        new("aaaaaaaa-0003-0000-0000-000000000000"),
        new("aaaaaaaa-0004-0000-0000-000000000000"),
        new("aaaaaaaa-0005-0000-0000-000000000000"),
        new("aaaaaaaa-0006-0000-0000-000000000000"),
        new("aaaaaaaa-0007-0000-0000-000000000000"),
        new("aaaaaaaa-0008-0000-0000-000000000000"),
        new("aaaaaaaa-0009-0000-0000-000000000000"),
        new("aaaaaaaa-000a-0000-0000-000000000000"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await featureManager.IsEnabledAsync(FeatureFlags.InboxDuplicateTestSeed))
        {
            logger.LogInformation("Feature flag '{FeatureFlag}' is disabled, skipping inbox duplicate test seed",
                FeatureFlags.InboxDuplicateTestSeed);
            return;
        }

        bool alreadySeeded = await dbContext.OutboxMessages
            .AnyAsync(m => m.Id == MessageIds[0], cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation("Inbox duplicate test messages already seeded, skipping");
            return;
        }

        logger.LogInformation("Seeding {Total} outbox messages for inbox deduplication test",
            MessageIds.Length);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var id in MessageIds)
        {
            dbContext.Database.ExecuteSql($"""
                INSERT INTO "orders"."outbox_messages"
                    ("id", "type", "destination", "content", "headers", "occurred_on_utc")
                VALUES (
                    {id},
                    'Shared.Contracts.Events.OrderCreatedIntegrationEvent, Shared.Contracts',
                    'order-created',
                    json_build_object(
                        'OrderId',     gen_random_uuid(),
                        'CustomerId',  gen_random_uuid(),
                        'TotalAmount', 50.00,
                        'ProductId',   '00000000-0000-0000-0000-000000000001',
                        'Quantity',    1
                    )::jsonb,
                    json_build_object('correlation-id', gen_random_uuid()::text)::jsonb,
                    NOW()
                )
                """);
        }

        logger.LogInformation("Inserted {Count} outbox messages", MessageIds.Length);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Outbox seed complete — {Count} messages inserted. " +
            "Inbox duplicate entries are seeded by Inventory.Consumer",
            MessageIds.Length);
    }
}
