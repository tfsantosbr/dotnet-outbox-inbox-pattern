namespace Shared.Inbox.Abstractions.Metrics;

internal interface IInboxMetrics
{
    void RecordRegistered();
    void RecordDuplicate();
    void RecordHandlerDuration(double milliseconds);
}