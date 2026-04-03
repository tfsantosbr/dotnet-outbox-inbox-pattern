namespace Shared.Outbox.Settings;

public record OutboxSettings
{
    public string ConnectionString { get; init; } = string.Empty;
    public string Schema { get; init; } = "public";
    public string TableName { get; init; } = "OutboxMessages";
    public int IntervalInSeconds { get; init; }
    public int MessagesBatchSize { get; init; }
}