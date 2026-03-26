namespace Shared.Events;

public interface IIntegrationEvent : IEventBase
{
    /// <summary>Correlation ID for distributed tracing across services.</summary>
    string CorrelationId { get; }

    /// <summary>ID of the command or event that caused this event. Null if this is the origin.</summary>
    string? CausationId { get; }

    /// <summary>Name of the service or module that produced this event.</summary>
    string Source { get; }
}
