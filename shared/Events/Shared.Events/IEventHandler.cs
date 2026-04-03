namespace Shared.Events;

public interface IEventHandler<in TEvent>
    where TEvent : IEventBase
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}