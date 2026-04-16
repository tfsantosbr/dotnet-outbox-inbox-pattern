using Shared.Events;
using Shared.Messaging.Abstractions.Models;

namespace Shared.Messaging.Abstractions.Interfaces;

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

    Task PublishBatchAsync(
        IReadOnlyList<MessageBatchItem> messages,
        CancellationToken cancellationToken = default);
}