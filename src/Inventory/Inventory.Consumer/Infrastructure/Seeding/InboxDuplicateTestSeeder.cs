using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Inventory.Consumer.Application.FeatureFlags;

namespace Inventory.Consumer.Infrastructure.Seeding;

/// <summary>
/// Pre-registers 4 inbox messages as already processed, to test the
/// Inbox Pattern deduplication logic.
///
/// These message IDs match the ones seeded by the InboxDuplicateTestSeeder
/// in Orders.API. Expected result after the outbox processor runs:
///   - 6 messages processed normally by the inventory consumer
///   - 4 messages detected as duplicates and discarded (stock NOT reduced twice)
/// </summary>
public class InboxDuplicateTestSeeder(
    InventoryDbContext dbContext,
    IFeatureManager featureManager,
    ILogger<InboxDuplicateTestSeeder> logger) : IDatabaseSeeder
{
    private const string InboxConsumer = "inventory.order-created-consumer";

    // Deterministic GUIDs matching the first 4 outbox messages seeded in Orders.API
    private static readonly Guid[] DuplicateIds =
    [
        new("aaaaaaaa-0001-0000-0000-000000000000"),
        new("aaaaaaaa-0002-0000-0000-000000000000"),
        new("aaaaaaaa-0003-0000-0000-000000000000"),
        new("aaaaaaaa-0004-0000-0000-000000000000"),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await featureManager.IsEnabledAsync(FeatureFlags.InboxDuplicateTestSeed))
        {
            logger.LogInformation("Feature flag '{FeatureFlag}' is disabled, skipping inbox duplicate test seed",
                FeatureFlags.InboxDuplicateTestSeed);
            return;
        }

        bool alreadySeeded = await dbContext.Database
            .SqlQuery<int>($"""
                SELECT 1 AS "Value" FROM "inventory"."inbox_messages"
                WHERE "message_id" = {DuplicateIds[0].ToString()} AND "consumer" = {InboxConsumer}
                """)
            .AnyAsync(cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation("Inbox duplicate test messages already seeded, skipping");
            return;
        }

        logger.LogInformation(
            "Seeding {Count} inbox messages as already processed to test deduplication",
            DuplicateIds.Length);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var id in DuplicateIds)
        {
            dbContext.Database.ExecuteSql($"""
                INSERT INTO "inventory"."inbox_messages" ("message_id", "consumer", "processed_on_utc")
                VALUES ({id.ToString()}, {InboxConsumer}, NOW())
                ON CONFLICT ("message_id", "consumer") DO NOTHING
                """);
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Inbox duplicate test seed complete — {Count} messages pre-registered as already processed: {Ids}",
            DuplicateIds.Length, string.Join(", ", DuplicateIds));
    }
}
