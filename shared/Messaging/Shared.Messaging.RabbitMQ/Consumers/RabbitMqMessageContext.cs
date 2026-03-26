using RabbitMQ.Client;
using Shared.Messaging.Abstractions;

namespace Shared.Messaging.RabbitMQ.Consumers;

internal sealed class RabbitMqMessageContext(IChannel channel, ulong deliveryTag) : IMessageContext
{
    public async Task AckAsync(bool multiple = false, CancellationToken cancellationToken = default)
        => await channel.BasicAckAsync(deliveryTag, multiple: multiple, cancellationToken);

    public async Task NackAsync(bool multiple = false, bool requeue = true, CancellationToken cancellationToken = default)
        => await channel.BasicNackAsync(deliveryTag, multiple: multiple, requeue: requeue, cancellationToken);
}
