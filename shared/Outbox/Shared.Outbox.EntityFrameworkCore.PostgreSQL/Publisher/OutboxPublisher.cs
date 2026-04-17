using Shared.Outbox.Abstractions.Database;
using Shared.Outbox.Abstractions.Interfaces;
using Shared.Outbox.Abstractions.Models;
using Shared.Events;
using Shared.Messaging.Abstractions.Interfaces;
using Shared.Messaging.Abstractions.Models;

namespace Shared.Outbox.EntityFrameworkCore.PostgreSQL.Publisher;

public sealed class OutboxPublisher<TContext>(TContext context, IPublishTopologyRegistry topologyRegistry)
    : IOutboxPublisher
    where TContext : IOutboxDbContext
{
    public Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TEvent : IEventBase
    {
        var options = GetPublishOptions<TEvent>();

        var outboxMessage = OutboxMessage.Create(
            options.Destination,
            integrationEvent.MessageId,
            integrationEvent,
            integrationEvent.OccurredOnUtc,
            headers
        );

        context.OutboxMessages.Add(outboxMessage);

        return Task.CompletedTask;
    }

    private PublishOptions GetPublishOptions<TEvent>() =>
        topologyRegistry.GetOptions(typeof(TEvent))
            ?? throw new InvalidOperationException(
                $"No PublishOptions registered for '{typeof(TEvent).Name}'. " +
                $"Call AddPublishOptions<{typeof(TEvent).Name}>() during messaging configuration.");
}