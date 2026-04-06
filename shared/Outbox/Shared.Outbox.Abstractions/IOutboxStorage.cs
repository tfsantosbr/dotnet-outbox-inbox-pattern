namespace Shared.Outbox.Abstractions;

public interface IOutboxStorage
{
    Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync(CancellationToken cancellationToken);
    Task UpdateMessageAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
