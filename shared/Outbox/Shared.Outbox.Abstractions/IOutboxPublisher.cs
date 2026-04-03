using Shared.Events;

namespace Shared.Outbox.Abstractions;

public interface IOutboxPublisher
{
    Task Publish<TEvent>(
        TEvent integrationEvent,
        string destination,
        IDictionary<string, string>? headers = null
    )
        where TEvent : IEventBase;
}