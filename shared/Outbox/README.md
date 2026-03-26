# Outbox

Reliable event publishing. Events are stored in an outbox table in the same transaction as domain changes, then published to the message bus by a background service.

## Architecture

- **Shared.Outbox.Abstractions**: `IOutboxPublisher`, `IOutboxDbContext`
- **Shared.Outbox**: Storage implementation, integrates with EF Core

## Main Abstractions

```csharp
public interface IOutboxPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IIntegrationEvent;
}

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
```

## Usage Example

```csharp
// In command handler (same transaction as SaveChangesAsync)
await _outboxPublisher.PublishAsync(new NotificationSentIntegrationEvent
{
    Id = Guid.NewGuid(),
    OccurredOn = DateTime.UtcNow,
    Subject = "Tab closed",
    Message = "...",
    Type = NotificationType.Email,
    Recipients = [...]
}, cancellationToken);

await _unitOfWork.SaveChangesAsync(cancellationToken);  // Outbox + domain in same transaction
```

Background service reads unprocessed outbox rows and publishes to RabbitMQ.
