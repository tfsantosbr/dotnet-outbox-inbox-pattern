namespace Shared.Messaging.Abstractions.Interfaces;

public interface IMessageContext
{
    IReadOnlyDictionary<string, string> Headers { get; }
    string? MessageId { get; }
    bool Redelivered { get; }
    Task AckAsync(bool multiple = false, CancellationToken cancellationToken = default);
    Task NackAsync(bool multiple = false, bool requeue = true, CancellationToken cancellationToken = default);
}