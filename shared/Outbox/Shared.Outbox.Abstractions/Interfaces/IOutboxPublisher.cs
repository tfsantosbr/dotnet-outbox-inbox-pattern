using Shared.Events;

namespace Shared.Outbox.Abstractions.Interfaces;

public interface IOutboxPublisher
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TEvent : IEventBase;
}