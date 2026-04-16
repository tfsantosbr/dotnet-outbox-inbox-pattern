namespace Shared.Messaging.Abstractions.Models;

public record MessageBatchItem(
    string Content,
    string Destination,
    IDictionary<string, string>? Headers);