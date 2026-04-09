namespace Shared.Outbox.Settings;

public record OutboxProcessorOptions
{
    public int IntervalInSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 30;
    public int MaxParallelism { get; set; } = 1;
}
