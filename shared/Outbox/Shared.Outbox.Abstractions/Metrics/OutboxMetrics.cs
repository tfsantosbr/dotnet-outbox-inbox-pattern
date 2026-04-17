using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shared.Outbox.Abstractions.Metrics;

internal sealed class OutboxMetrics : IOutboxMetrics, IDisposable
{
    public const string MeterName = "Outbox";

    private readonly Meter _meter;
    private readonly Counter<long> _published;
    private readonly Counter<long> _failed;
    private readonly Counter<long> _processed;
    private readonly Histogram<double> _fetchDuration;
    private readonly Histogram<double> _updateDuration;
    private readonly Histogram<double> _publishDuration;
    private readonly Histogram<double> _cycleDuration;
    private readonly Histogram<long> _batchSize;
    private readonly IReadOnlyDictionary<string, string>? _globalTags;

    public OutboxMetrics(IMeterFactory meterFactory, IReadOnlyDictionary<string, string>? globalTags = null)
    {
        _meter = meterFactory.Create(MeterName);
        _globalTags = globalTags;

        _published = _meter.CreateCounter<long>(
            "outbox.messages.published",
            unit: "{message}",
            description: "Number of outbox messages successfully published");

        _failed = _meter.CreateCounter<long>(
            "outbox.messages.failed",
            unit: "{message}",
            description: "Number of outbox messages that failed to publish");

        _processed = _meter.CreateCounter<long>(
            "outbox.messages.processed",
            unit: "{message}",
            description: "Total number of outbox messages processed, regardless of outcome");

        _fetchDuration = _meter.CreateHistogram<double>(
            "outbox.fetch.duration",
            unit: "ms",
            description: "Time taken to fetch a batch of outbox messages from the database");

        _updateDuration = _meter.CreateHistogram<double>(
            "outbox.update.duration",
            unit: "ms",
            description: "Time taken to batch update outbox messages in the database");

        _publishDuration = _meter.CreateHistogram<double>(
            "outbox.publish.duration",
            unit: "ms",
            description: "Time taken to publish a batch of messages to the message broker");

        _cycleDuration = _meter.CreateHistogram<double>(
            "outbox.cycle.duration",
            unit: "ms",
            description: "Total time taken for one full outbox processing cycle");

        _batchSize = _meter.CreateHistogram<long>(
            "outbox.batch.size",
            unit: "{message}",
            description: "Number of messages processed per outbox cycle");
    }

    public void RecordPublished(IReadOnlyDictionary<string, string>? tags = null) =>
        _published.Add(1, BuildTagList(tags));

    public void RecordFailed(IReadOnlyDictionary<string, string>? tags = null) =>
        _failed.Add(1, BuildTagList(tags));

    public void RecordProcessed(IReadOnlyDictionary<string, string>? tags = null) =>
        _processed.Add(1, BuildTagList(tags));

    public void RecordFetchDuration(double milliseconds, IReadOnlyDictionary<string, string>? tags = null) =>
        _fetchDuration.Record(milliseconds, BuildTagList(tags));

    public void RecordUpdateDuration(double milliseconds, IReadOnlyDictionary<string, string>? tags = null) =>
        _updateDuration.Record(milliseconds, BuildTagList(tags));

    public void RecordPublishDuration(double milliseconds, IReadOnlyDictionary<string, string>? tags = null) =>
        _publishDuration.Record(milliseconds, BuildTagList(tags));

    public void RecordCycleDuration(double milliseconds, IReadOnlyDictionary<string, string>? tags = null) =>
        _cycleDuration.Record(milliseconds, BuildTagList(tags));

    public void RecordBatchSize(long count, IReadOnlyDictionary<string, string>? tags = null) =>
        _batchSize.Record(count, BuildTagList(tags));

    private TagList BuildTagList(IReadOnlyDictionary<string, string>? additionalTags)
    {
        var tagList = new TagList();

        if (_globalTags is not null)
            foreach (var tag in _globalTags)
                tagList.Add(tag.Key, tag.Value);

        if (additionalTags is not null)
            foreach (var tag in additionalTags)
                tagList.Add(tag.Key, tag.Value);

        return tagList;
    }

    public void Dispose() => _meter.Dispose();
}