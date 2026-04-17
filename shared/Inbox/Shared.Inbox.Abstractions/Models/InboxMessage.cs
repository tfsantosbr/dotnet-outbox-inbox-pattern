namespace Shared.Inbox.Abstractions.Models;

public sealed class InboxMessage
{
    private InboxMessage(string messageId, string consumer)
    {
        MessageId = messageId;
        Consumer = consumer;
    }

    private InboxMessage() { }

    public string MessageId { get; init; } = string.Empty;
    public string Consumer { get; init; } = string.Empty;
    public DateTime? ProcessedOnUtc { get; private set; }
    public DateTime? ErrorHandledOnUtc { get; private set; }
    public string? Error { get; private set; }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        Error = null;
        ErrorHandledOnUtc = null;
    }

    public void MarkAsProcessedWithError(string error)
    {
        ProcessedOnUtc = DateTime.UtcNow;
        ErrorHandledOnUtc = DateTime.UtcNow;
        Error = error;
    }

    public static InboxMessage Create(string messageId, string consumer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);
        
        return new InboxMessage(messageId, consumer);
    }
}