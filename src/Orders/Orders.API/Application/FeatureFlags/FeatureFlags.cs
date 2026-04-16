namespace Orders.API.Application.FeatureFlags;

public static class FeatureFlags
{
    public const string OutboxStressTestSeed = nameof(OutboxStressTestSeed);
    public const string InboxDuplicateTestSeed = nameof(InboxDuplicateTestSeed);
}
