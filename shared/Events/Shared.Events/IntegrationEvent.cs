namespace Shared.Events;

public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent(DateTime occurredOnUtc, string correlationId, string? causationId, string source)
    {
        OccurredOnUtc = occurredOnUtc;
        CorrelationId = correlationId;
        CausationId = causationId;
        Source = source;
    }

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOnUtc { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string? CausationId { get; init; }
    public string Source { get; init; } = string.Empty;
}
