namespace Shared.Outbox.Settings;

public record OutboxSettings
{
    public string ConnectionString { get; init; } = string.Empty;
    public int IntervalInSeconds { get; init; }
    public int MessagesBatchSize { get; init; }
}
