namespace Shared.Messaging.Abstractions;

public interface IMessageContext
{
    Task AckAsync(bool multiple = false, CancellationToken cancellationToken = default);
    Task NackAsync(bool multiple = false, bool requeue = true, CancellationToken cancellationToken = default);
}
