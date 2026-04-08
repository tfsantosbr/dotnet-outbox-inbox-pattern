namespace Shared.Outbox.Metrics;

public sealed class OutboxMetricsOptions
{
    public IReadOnlyDictionary<string, string>? Tags { get; set; }
}
