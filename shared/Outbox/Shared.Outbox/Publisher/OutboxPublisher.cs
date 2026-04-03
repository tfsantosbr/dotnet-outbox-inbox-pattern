using Shared.Events;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;

namespace Shared.Outbox.Publisher;

public class OutboxPublisher<TContext>(TContext context) : IOutboxPublisher
    where TContext : IOutboxDbContext
{
    public async Task Publish<TEvent>(
        TEvent integrationEvent,
        string destination,
        IDictionary<string, string>? headers = null
    )
        where TEvent : IEventBase
    {
        var outboxMessage = OutboxMessage.Create(
            destination,
            integrationEvent.MessageId,
            integrationEvent,
            integrationEvent.OccurredOnUtc,
            headers
        );

        await context.OutboxMessages.AddAsync(outboxMessage);
    }
}