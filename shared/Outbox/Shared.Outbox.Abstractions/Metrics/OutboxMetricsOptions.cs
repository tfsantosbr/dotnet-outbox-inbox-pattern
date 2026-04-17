namespace Shared.Outbox.Abstractions.Metrics;

public sealed class OutboxMetricsOptions
{
    public IReadOnlyDictionary<string, string>? Tags { get; set; }
}