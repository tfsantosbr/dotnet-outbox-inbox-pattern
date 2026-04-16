namespace Shared.Messaging.Abstractions.Models;

public static class MessageHeaders
{
    public const string MessageId = "message-id";
    public const string OccurredOnUtc = "occurred-on-utc";
    public const string CorrelationId = "correlation-id";
    public const string CausationId = "causation-id";
    public const string Source = "source";
}