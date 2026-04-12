namespace InboxPattern.Abstractions.Models;

public sealed class InboxMessage
{
    private InboxMessage(string messageId, string consumer, DateTime processedOnUtc)
    {
        MessageId = messageId;
        Consumer = consumer;
        ProcessedOnUtc = processedOnUtc;
    }

    private InboxMessage() { }

    public string MessageId { get; init; } = string.Empty;
    public string Consumer { get; init; } = string.Empty;
    public DateTime ProcessedOnUtc { get; init; }

    public static InboxMessage Create(string messageId, string consumer, DateTime processedOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);
        return new InboxMessage(messageId, consumer, processedOnUtc);
    }
}
