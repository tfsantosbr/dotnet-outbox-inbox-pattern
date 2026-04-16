using Shared.Messaging.Abstractions.Models;

namespace Shared.Messaging.Abstractions.Interfaces;

public interface IMessageConsumer<in TMessage>
{
    Task<ConsumerResult> ConsumeAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken = default);
}