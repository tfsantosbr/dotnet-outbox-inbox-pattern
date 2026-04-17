using System.Diagnostics.Metrics;

namespace Shared.Inbox.Abstractions.Metrics;

internal sealed class InboxMetrics : IInboxMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _registered;
    private readonly Counter<long> _duplicate;
    private readonly Histogram<double> _handlerDuration;

    public InboxMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(InboxInstrumentation.MeterName);

        _registered = _meter.CreateCounter<long>(
            "inbox.messages.registered",
            unit: "{message}",
            description: "Number of inbox messages successfully registered (first delivery)");

        _duplicate = _meter.CreateCounter<long>(
            "inbox.messages.duplicate",
            unit: "{message}",
            description: "Number of inbox messages rejected as duplicates");

        _handlerDuration = _meter.CreateHistogram<double>(
            "inbox.handler.duration",
            unit: "ms",
            description: "Time taken to execute the business handler for an inbox message");
    }

    public void RecordRegistered() => _registered.Add(1);

    public void RecordDuplicate() => _duplicate.Add(1);

    public void RecordHandlerDuration(double milliseconds) => _handlerDuration.Record(milliseconds);

    public void Dispose() => _meter.Dispose();
}