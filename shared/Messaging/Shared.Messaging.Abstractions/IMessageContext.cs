namespace Shared.Messaging.Abstractions;

public interface IMessageContext
{
    IReadOnlyDictionary<string, string> Headers { get; }
    Task AckAsync(bool multiple = false, CancellationToken cancellationToken = default);
    Task NackAsync(bool multiple = false, bool requeue = true, CancellationToken cancellationToken = default);
}
