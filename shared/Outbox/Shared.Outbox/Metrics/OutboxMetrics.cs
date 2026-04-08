using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shared.Outbox.Metrics;

internal sealed class OutboxMetrics : IOutboxMetrics, IDisposable
{
    public const string MeterName = "Shared.Outbox";

    private readonly Meter _meter;
    private readonly Counter<long> _published;
    private readonly Counter<long> _failed;
    private readonly Counter<long> _processed;
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
    }

    public void RecordPublished(IReadOnlyDictionary<string, string>? tags = null) =>
        _published.Add(1, BuildTagList(tags));

    public void RecordFailed(IReadOnlyDictionary<string, string>? tags = null) =>
        _failed.Add(1, BuildTagList(tags));

    public void RecordProcessed(IReadOnlyDictionary<string, string>? tags = null) =>
        _processed.Add(1, BuildTagList(tags));

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
