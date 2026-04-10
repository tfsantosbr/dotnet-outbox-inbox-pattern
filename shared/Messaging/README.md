# Messaging

Message bus abstraction and RabbitMQ implementation for publishing and consuming integration events. Used by the Outbox background processor and by consumers in other modules.

## Architecture

| Project | Responsibility |
| --- | --- |
| `Shared.Messaging.Abstractions` | `IMessageBus`, `IMessageConsumer<T>`, `IMessageContext`, `MessageHeaders` |
| `Shared.Messaging.RabbitMQ` | RabbitMQ implementation, connection management, consumer worker |

---

## Core Abstractions

### `IMessageBus`

```csharp
public interface IMessageBus
{
    // Publish a strongly-typed event (destination resolved from topology registry)
    Task PublishAsync<TMessage>(
        TMessage message,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TMessage : IEventBase;

    // Publish a raw JSON payload to an explicit destination
    Task PublishAsync(
        string message,
        string destination,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}
```

### `IMessageConsumer<TMessage>`

```csharp
public interface IMessageConsumer<in TMessage>
{
    Task ConsumeAsync(TMessage message, IMessageContext context, CancellationToken cancellationToken = default);
}
```

### `IMessageContext`

Provides delivery metadata and manual acknowledgment control:

```csharp
public interface IMessageContext
{
    IReadOnlyDictionary<string, string> Headers { get; }
    string? MessageId { get; }
    bool Redelivered { get; }

    Task AckAsync(bool multiple = false, CancellationToken cancellationToken = default);
    Task NackAsync(bool multiple = false, bool requeue = true, CancellationToken cancellationToken = default);
}
```

### `MessageHeaders`

Standard header keys used across all messages:

```csharp
public static class MessageHeaders
{
    public const string MessageId     = "message-id";
    public const string OccurredOnUtc = "occurred-on-utc";
    public const string CorrelationId = "correlation-id";
    public const string CausationId   = "causation-id";
    public const string Source        = "source";
}
```

The RabbitMQ implementation enforces the presence of `message-id` and `occurred-on-utc` at publish time.

---

## Configuration

### Options

**`RabbitMqOptions`** — connection settings:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ConnectionString` | `string` | — | RabbitMQ connection URI, e.g. `amqp://guest:guest@localhost:5672` |
| `PublisherConfirmationsEnabled` | `bool` | `true` | Enables AMQP publisher confirms. The broker acknowledges each published message, ensuring at-least-once delivery. When `false`, publishes are fire-and-forget — if the broker crashes after receiving the bytes, the message is silently lost with no exception. |
| `PublisherConfirmationTrackingEnabled` | `bool` | `true` | Enables per-message confirmation tracking so individual publish failures are correlated back to their originating call. Requires `PublisherConfirmationsEnabled = true`. |

> **Recommendation:** keep both properties at their default (`true`) when using the Outbox processor. The Outbox marks messages as processed only after a successful publish — if a publish silently succeeds without broker confirmation, a lost message will never be retried.
>
> Set both to `false` only when throughput is the priority and message loss is acceptable (e.g. metrics, telemetry, non-critical events).

```csharp
.UseRabbitMq(o =>
{
    o.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!;

    // Default: true — broker confirms every publish (at-least-once delivery)
    o.PublisherConfirmationsEnabled = true;
    o.PublisherConfirmationTrackingEnabled = true;

    // Set to false for fire-and-forget / maximum throughput scenarios
    // o.PublisherConfirmationsEnabled = false;
    // o.PublisherConfirmationTrackingEnabled = false;
})
```

**`RabbitMqPublishOptions`** — publish topology per message type:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Destination` | `string` | — | Exchange name on RabbitMQ |
| `RoutingKey` | `string` | `""` | Routing key (used by Direct/Topic exchanges) |
| `ExchangeType` | `RabbitMqExchangeType` | `Fanout` | `Fanout`, `Direct`, `Topic`, `Headers` |
| `Durable` | `bool` | `false` | Whether the exchange survives broker restart |

**`RabbitMqConsumerOptions`** — consumer topology:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Exchange` | `string` | `""` | Exchange to bind to |
| `Queue` | `string` | `""` | Queue name to consume from |
| `RoutingKey` | `string` | `""` | Binding routing key |
| `ExchangeType` | `RabbitMqExchangeType` | `Fanout` | Exchange type |
| `Durable` | `bool` | `false` | Durable queue |
| `Exclusive` | `bool` | `false` | Exclusive queue |
| `AutoDelete` | `bool` | `false` | Delete queue when no consumers |
| `AckMode` | `AckMode` | `Manual` | `Manual` or `AutoOnSuccess` |

---

## Setup

Call `AddMessaging()` in `Program.cs`, then chain `UseRabbitMq`, `AddPublishOptions`, and `AddConsumer`:

```csharp
using Shared.Messaging.Abstractions;
using Shared.Messaging.RabbitMQ.Extensions;
using Shared.Messaging.RabbitMQ.Options;

builder.Services
    .AddMessaging()
    .UseRabbitMq(o =>
    {
        o.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!;
    })
    // Register the publish topology for each outbound message type
    .AddPublishOptions<OrderCreatedIntegrationEvent>(o =>
    {
        o.Destination   = "orders-created";
        o.ExchangeType  = RabbitMqExchangeType.Fanout;
        o.Durable       = true;
    })
    // Register a consumer (creates a BackgroundService worker automatically)
    .AddConsumer<OrderCreatedConsumer, OrderCreatedIntegrationEvent>(o =>
    {
        o.Exchange     = "orders-created";
        o.Queue        = "inventory.orders";
        o.ExchangeType = RabbitMqExchangeType.Fanout;
        o.Durable      = true;
        o.AckMode      = AckMode.AutoOnSuccess;
    });
```

### `AddPublishOptions<TMessage>`

Registers the routing topology for a message type in `IPublishTopologyRegistry`. Required when using the strongly-typed `IMessageBus.PublishAsync<TMessage>` overload (including via the Outbox processor).

### `AddConsumer<TConsumer, TMessage>`

Registers `TConsumer` as a scoped service and starts a `RabbitMqConsumerWorker<TMessage, TConsumer>` background service that declares the exchange/queue, binds them, and dispatches messages to `TConsumer.ConsumeAsync`.

---

## Publishing

> Prefer the **Outbox** library for any publish that must be atomic with a database write. Use direct publishing only for fire-and-forget scenarios where exactly-once guarantees are not required.

```csharp
public class SomeService(IMessageBus messageBus)
{
    public async Task DoWorkAsync(CancellationToken ct)
    {
        var @event = new OrderCreatedIntegrationEvent(...);

        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.CorrelationId] = correlationId,
            [MessageHeaders.CausationId]   = causationId,
            [MessageHeaders.Source]        = "orders-api",
        };

        await messageBus.PublishAsync(@event, headers, ct);
    }
}
```

---

## Consuming

Implement `IMessageConsumer<TMessage>` and register it with `AddConsumer`:

```csharp
public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public async Task ConsumeAsync(
        OrderCreatedIntegrationEvent message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Order received: {OrderId}", message.OrderId);

        // ... process message ...

        // With AckMode.Manual, acknowledge explicitly:
        await context.AckAsync(cancellationToken: cancellationToken);
    }
}
```

With `AckMode.AutoOnSuccess`, the worker calls `AckAsync` automatically when `ConsumeAsync` completes without throwing. On exception, it calls `NackAsync` with `requeue: true`.

---

## Registered Services

| Service | Lifetime | Notes |
| --- | --- | --- |
| `IMessageBus` | Singleton | `RabbitMqMessageBus` |
| `IPersistentRabbitMqConnection` | Singleton | Shared connection with lazy init |
| `IPublishTopologyRegistry` | Singleton | Maps message types to `PublishOptions` |
| `TConsumer` | Scoped | One scope per message delivery |
| `RabbitMqConsumerWorker<T,C>` | Hosted | One worker per `AddConsumer` call |
