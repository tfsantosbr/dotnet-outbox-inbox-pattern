namespace Shared.Messaging.Abstractions;

public interface IMessageConsumer<in TMessage>
{
    Task ConsumeAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken = default);
}