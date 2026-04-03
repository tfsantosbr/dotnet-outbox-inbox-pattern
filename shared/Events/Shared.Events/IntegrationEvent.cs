namespace Shared.Events;

public abstract record IntegrationEvent : IEventBase
{
    public Guid MessageId { get; init; } = Guid.CreateVersion7();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;

}