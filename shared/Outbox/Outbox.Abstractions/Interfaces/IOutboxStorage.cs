using Outbox.Abstractions.Models;

namespace Outbox.Abstractions.Interfaces;

public interface IOutboxStorage
{
    Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync(CancellationToken cancellationToken);
    Task UpdateMessagesAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}