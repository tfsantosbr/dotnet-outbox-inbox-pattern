using Shared.Events;

namespace Shared.Messaging.Abstractions;

public interface IMessageBus
{
    Task PublishAsync<TMessage>(
        TMessage message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TMessage : IEventBase;

    Task PublishAsync(
        string message,
        string destination,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}