namespace Shared.Messaging.Abstractions;

public record MessageBatchItem(
    string Content,
    string Destination,
    IDictionary<string, string>? Headers);
