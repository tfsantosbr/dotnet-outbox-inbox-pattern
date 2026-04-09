namespace Shared.Outbox.Abstractions;

public interface IOutboxStorage
{
    Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync(CancellationToken cancellationToken);
    Task UpdateMessagesAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
