namespace Shared.Inbox.Abstractions.Metrics;

public sealed class InboxMetricsOptions
{
    public IReadOnlyDictionary<string, string>? Tags { get; set; }
}