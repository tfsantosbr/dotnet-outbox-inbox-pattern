namespace Shared.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(T @event, string exchange, CancellationToken cancellationToken = default);
}
