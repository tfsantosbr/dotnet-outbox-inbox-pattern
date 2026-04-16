using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Orders.API.Application.FeatureFlags;

namespace Orders.API.Infrastructure.Seeding;

/// <summary>
/// Seeds outbox messages to test both the Inbox Pattern deduplication logic and the
/// dead-letter queue behaviour of the OrderCreatedConsumer in Inventory.Consumer.
///
/// Message groups:
///   aaaaaaaa-* (10 messages) — valid ProductId (00000000-…-0001).
///     Duplicate inbox entries for these are seeded by InboxDuplicateTestSeeder in Inventory.Consumer.
///     Expected result:
///       - 6 messages processed normally (stock reduced)
///       - 4 messages detected as duplicates and discarded
///
///   bbbbbbbb-* (3 messages) — non-existent ProductIds.
///     The OrderCreatedConsumer returns Nack(requeue: false) for each, which routes
///     them to the dead-letter queue without retrying.
/// </summary>
public class InboxDuplicateTestSeeder(
    OrdersDbContext dbContext,
    IFeatureManager featureManager,
    ILogger<InboxDuplicateTestSeeder> logger) : IDatabaseSeeder
{
    // Deterministic GUIDs so the seed is idempotent

    // Valid product — deduplication scenario
    private static readonly Guid[] DeduplicationMessageIds =
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

    // Non-existent products — dead-letter scenario
    private static readonly (Guid MessageId, Guid ProductId)[] DeadLetterMessages =
    [
        (new("bbbbbbbb-0001-0000-0000-000000000000"), new("dddddddd-0001-0000-0000-000000000000")),
        (new("bbbbbbbb-0002-0000-0000-000000000000"), new("dddddddd-0002-0000-0000-000000000000")),
        (new("bbbbbbbb-0003-0000-0000-000000000000"), new("dddddddd-0003-0000-0000-000000000000")),
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
            .AnyAsync(m => m.Id == DeduplicationMessageIds[0], cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation("Inbox duplicate test messages already seeded, skipping");
            return;
        }

        int totalMessages = DeduplicationMessageIds.Length + DeadLetterMessages.Length;

        logger.LogInformation("Seeding {Total} outbox messages for inbox deduplication and dead-letter tests",
            totalMessages);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var id in DeduplicationMessageIds)
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

        logger.LogInformation("Inserted {Count} deduplication outbox messages", DeduplicationMessageIds.Length);

        foreach (var (messageId, productId) in DeadLetterMessages)
        {
            dbContext.Database.ExecuteSql($"""
                INSERT INTO "orders"."outbox_messages"
                    ("id", "type", "destination", "content", "headers", "occurred_on_utc")
                VALUES (
                    {messageId},
                    'Shared.Contracts.Events.OrderCreatedIntegrationEvent, Shared.Contracts',
                    'order-created',
                    json_build_object(
                        'OrderId',     gen_random_uuid(),
                        'CustomerId',  gen_random_uuid(),
                        'TotalAmount', 50.00,
                        'ProductId',   {productId},
                        'Quantity',    1
                    )::jsonb,
                    json_build_object('correlation-id', gen_random_uuid()::text)::jsonb,
                    NOW()
                )
                """);
        }

        logger.LogInformation(
            "Inserted {Count} dead-letter outbox messages (non-existent ProductIds)",
            DeadLetterMessages.Length);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Outbox seed complete — {Total} messages inserted ({Dedup} deduplication + {DL} dead-letter). " +
            "Inbox duplicate entries are seeded by Inventory.Consumer",
            totalMessages, DeduplicationMessageIds.Length, DeadLetterMessages.Length);
    }
}
