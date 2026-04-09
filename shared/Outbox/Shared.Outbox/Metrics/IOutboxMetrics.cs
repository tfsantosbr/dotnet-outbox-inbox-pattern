namespace Shared.Outbox.Metrics;

internal interface IOutboxMetrics
{
    void RecordPublished(IReadOnlyDictionary<string, string>? tags = null);
    void RecordFailed(IReadOnlyDictionary<string, string>? tags = null);
    void RecordProcessed(IReadOnlyDictionary<string, string>? tags = null);
    void RecordFetchDuration(double milliseconds, IReadOnlyDictionary<string, string>? tags = null);
    void RecordUpdateDuration(double milliseconds, IReadOnlyDictionary<string, string>? tags = null);
}
