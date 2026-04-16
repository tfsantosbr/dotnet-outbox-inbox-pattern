using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Orders.API.Application.FeatureFlags;

namespace Orders.API.Infrastructure.Seeding;

/// <summary>
/// Seeds 10 outbox messages and pre-registers 4 of them in the inbox
/// as already processed, to test the Inbox Pattern deduplication logic.
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
    private const string InboxConsumer = "inventory.order-created";

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

    // These 4 IDs will be pre-inserted in the inbox as already processed
    private static readonly Guid[] DuplicateIds = MessageIds[..4];

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

        logger.LogInformation(
            "Seeding {Total} outbox messages — {Duplicates} pre-registered in inbox as duplicates, {Normal} will be processed normally",
            MessageIds.Length, DuplicateIds.Length, MessageIds.Length - DuplicateIds.Length);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var id in MessageIds)
        {
            dbContext.Database.ExecuteSql($"""
                INSERT INTO "orders"."outbox_messages"
                    ("Id", "Type", "Destination", "Content", "Headers", "OccurredOnUtc")
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

        foreach (var id in DuplicateIds)
        {
            dbContext.Database.ExecuteSql($"""
                INSERT INTO "inventory"."inbox_messages" ("message_id", "consumer", "processed_on_utc")
                VALUES ({id.ToString()}, {InboxConsumer}, NOW())
                ON CONFLICT ("message_id", "consumer") DO NOTHING
                """);
        }

        logger.LogInformation(
            "Pre-registered {Count} inbox entries as already processed: {Ids}",
            DuplicateIds.Length, string.Join(", ", DuplicateIds));

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Inbox duplicate test seed complete — run the stack and watch the consumer logs: " +
            "4 messages should be discarded as duplicates, 6 should be processed normally");
    }
}
