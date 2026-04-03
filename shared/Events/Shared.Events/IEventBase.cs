namespace Shared.Events;

public interface IEventBase
{
    Guid MessageId { get; }
    DateTime OccurredOnUtc { get; }
}