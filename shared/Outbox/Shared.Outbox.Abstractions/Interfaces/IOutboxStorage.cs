using Shared.Outbox.Abstractions.Models;

namespace Shared.Outbox.Abstractions.Interfaces;

public interface IOutboxStorage
{
    Task<IReadOnlyList<OutboxMessage>> GetMessagesAsync(CancellationToken cancellationToken);
    Task UpdateMessagesAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}