# Messaging

Message bus abstraction for RabbitMQ. Used by Outbox and Inbox for publishing and consuming integration events.

## Architecture

- **Shared.Messaging.Abstractions**: `IMessageBus` interface
- **Shared.Messaging**: RabbitMQ implementation, `IMessageBusConnectionFactory`

## Main Abstractions

```csharp
public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}

public interface IMessageBusConnectionFactory
{
    // Connection management for RabbitMQ
}
```

## Usage Example

Typically not used directly by application code. Outbox background service and Inbox consumers use it. For direct publishing (outside Outbox):

```csharp
await _messageBus.PublishAsync(integrationEvent, cancellationToken);
```

Prefer Outbox for transactional consistency with database writes.
