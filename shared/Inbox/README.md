# Inbox

Idempotent message consumption using the Transactional Inbox Pattern. Each message is registered in an inbox table within the **same database transaction** as the business operation. Duplicate deliveries are detected via a unique constraint and silently acknowledged, ensuring exactly-once processing semantics.

## How It Works

```text
RabbitMQ delivers message
        │
InboxConsumerDecorator (wraps your consumer)
        │
        ├─ Open DB transaction
        ├─ INSERT INTO inbox_messages (message_id, consumer)
        │   ON CONFLICT DO NOTHING
        │
        ├─ Duplicate? ──► Commit + Ack (skip processing)
        │
        └─ New message ──► Call inner consumer
                                │
                          ┌─────┴─────┐
                        Ack         Nack
                          │           │
                    Mark processed  Mark error
                    Commit          Commit
                    Return Ack      Return Nack
```

## Project Structure

| Project | Responsibility |
| --- | --- |
| `Shared.Inbox.Abstractions` | `IInboxStorage`, `IInboxDbContext`, `InboxMessage`, `InboxConsumerDecorator<T>` |
| `Shared.Inbox.EntityFrameworkCore.PostgreSQL` | PostgreSQL storage implementation, `InboxStorageOptions` |

---

## Step-by-Step Integration Guide

### 1. Implement `IInboxDbContext` on your DbContext

Your `DbContext` must implement `IInboxDbContext` to expose the `InboxMessages` table.

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.Inbox.Abstractions.Database;
using Shared.Inbox.Abstractions.Models;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : DbContext(options), IInboxDbContext
{
    public DbSet<Order> Orders { get; init; }
    public DbSet<InboxMessage> InboxMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

        // Register the inbox table configuration.
        // Optionally pass a custom table name (default: "inbox_messages").
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration("inbox_messages"));
    }
}
```

`IInboxDbContext` interface:

```csharp
public interface IInboxDbContext
{
    DbSet<InboxMessage> InboxMessages { get; }
}
```

`InboxMessageEntityConfiguration` maps the entity to a PostgreSQL table with a composite primary key `(message_id, consumer)`, which is the uniqueness constraint that prevents duplicate processing.

---

### 2. Add the EF Core Migration

After configuring the `DbContext`, generate the migration to create the inbox table:

```bash
dotnet ef migrations add AddInboxMessageTable \
  --project src/Orders/Orders.API \
  --context OrdersDbContext
```

The migration will create a table with the following columns:

| Column | Type | Description |
| --- | --- | --- |
| `message_id` | `varchar(200)` | Broker message ID (from `IMessageContext.MessageId`) |
| `consumer` | `varchar(200)` | Consumer name (from `RabbitMqConsumerOptions.ConsumerName`) |
| `processed_on_utc` | `timestamp?` | When processing completed (`null` = not yet processed) |
| `error_handled_on_utc` | `timestamp?` | When the last error occurred |
| `error` | `text?` | Last error message |

**Primary Key:** composite `(message_id, consumer)` — uniqueness is enforced per consumer, not globally, so the same message can be consumed independently by different consumers.

---

### 3. Register the Inbox Services

In `Program.cs`, call `AddInbox()` and chain `UsePostgreSQLStorage`. This must be done **before** registering consumers with `AddInboxConsumer`.

```csharp
using Shared.Inbox.Abstractions.Extensions;
using Shared.Inbox.EntityFrameworkCore.PostgreSQL.Extensions;

builder.Services
    .AddInbox()
    .UsePostgreSQLStorage<OrdersDbContext>(o =>
    {
        o.Schema    = "orders";           // default: "public"
        o.TableName = "inbox_messages";   // must match InboxMessageEntityConfiguration
    });
```

> Unlike the Outbox library, the Inbox storage reuses the existing EF Core `DbContext` and its active connection. No additional connection string is needed.

#### `InboxBuilder` options

| Method | Description |
| --- | --- |
| `.UsePostgreSQLStorage<TContext>(configure?)` | Sets `Schema` and `TableName` for the inbox table |
| `.WithMetrics(configure?)` | Opt-in delivery metrics (see [Metrics](#metrics)) |

#### What Gets Registered

| Service | Lifetime | Notes |
| --- | --- | --- |
| `IInboxStorage` | Scoped | PostgreSQL storage, reuses the scoped `TContext` |

---

### 4. Register Consumers with `AddInboxConsumer`

Instead of `AddConsumer`, use `AddInboxConsumer` from the Messaging library. It wraps your consumer with `InboxConsumerDecorator<TMessage>` transparently.

```csharp
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;
using Shared.Messaging.RabbitMQ.Options;

builder.Services
    .AddMessaging()
    .UseRabbitMq(o => o.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!)
    .AddInboxConsumer<OrderCreatedConsumer, OrderCreatedIntegrationEvent, OrdersDbContext>(o =>
    {
        o.Exchange     = "orders-created";
        o.Queue        = "inventory.orders-created";
        o.ConsumerName = "inventory.orders-created-consumer";  // used as inbox consumer identifier — must be unique per queue
        o.ExchangeType = RabbitMqExchangeType.Fanout;
        o.Durable      = true;
        o.AckMode      = AckMode.Manual;
    });
```

`ConsumerName` becomes the `consumer` column in the inbox table. It must be consistent across restarts and unique per logical consumer. Use the queue name as a convention.

#### `AddConsumer` vs `AddInboxConsumer`

| | `AddConsumer` | `AddInboxConsumer` |
| --- | --- | --- |
| Duplicate protection | None | Yes — via inbox table |
| Idempotent processing | Manual | Automatic |
| Database transaction | Not required | Required (EF Core DbContext) |
| Use when | Fire-and-forget or already idempotent | At-least-once delivery with business side-effects |

---

### 5. Implement the Consumer

Your consumer is identical to one registered with `AddConsumer`. The `InboxConsumerDecorator` handles all duplicate detection — your handler only runs for new messages.

```csharp
public class OrderCreatedConsumer(InventoryService inventoryService)
    : IMessageConsumer<OrderCreatedIntegrationEvent>
{
    public async Task<ConsumerResult> ConsumeAsync(
        OrderCreatedIntegrationEvent message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        await inventoryService.ReserveStockAsync(message.OrderId, message.Items, cancellationToken);

        return ConsumerResult.Ack();
        // or: return ConsumerResult.Nack(requeue: false, error: "out of stock");
    }
}
```

> The consumer **must not** open its own database transaction. The `InboxConsumerDecorator` manages the transaction that wraps both the inbox registration and the business operation.

---

## How `InboxConsumerDecorator` Works

For each incoming message, the decorator:

1. Reads `context.MessageId` — if absent, discards the message with `Nack(requeue: false)`.
2. Opens an EF Core transaction via `dbContext.Database.BeginTransactionAsync`.
3. Executes `INSERT INTO inbox_messages (message_id, consumer) ON CONFLICT DO NOTHING`.
   - **0 rows affected** → duplicate → commits and returns `Ack()`.
   - **1 row affected** → new message → continues.
4. Calls `innerConsumer.ConsumeAsync`.
5. Updates the inbox row with `processed_on_utc` (success) or `error_handled_on_utc` + `error` (failure).
6. Commits the transaction and returns the consumer result.
7. On unhandled exception: marks the row as failed, commits, then re-throws (causes `Nack(requeue: true)` in the worker).

---

## Metrics

The inbox exposes opt-in delivery metrics via `System.Diagnostics.Metrics` (BCL).

### Enabling metrics

Call `.WithMetrics()` on the inbox builder:

```csharp
builder.Services
    .AddInbox()
    .UsePostgreSQLStorage<OrdersDbContext>()
    .WithMetrics();
```

To add extra global tags applied to every measurement:

```csharp
.WithMetrics(o =>
{
    o.Tags = new Dictionary<string, string>
    {
        ["environment"] = "production",
        ["service"]     = "inventory"
    };
});
```

### Subscribing the meter (OpenTelemetry)

Add the meter name to the OpenTelemetry metrics configuration in your service:

```csharp
using Shared.Inbox.Abstractions.Metrics;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(InboxInstrumentation.MeterName));  // "Inbox"
```

### Available instruments

| Instrument | Type | Unit | Description |
| --- | --- | --- | --- |
| `inbox.messages.registered` | Counter | `{message}` | Messages accepted as new (first delivery) |
| `inbox.messages.duplicate` | Counter | `{message}` | Messages rejected as duplicates |
| `inbox.handler.duration` | Histogram | `ms` | Time taken to execute the inner consumer handler |

### Grafana — useful queries

#### New vs duplicate messages

```promql
rate(inbox_messages_registered_total[1m])
rate(inbox_messages_duplicate_total[1m])
```

#### Duplicate rate (fraction of total deliveries that are duplicates)

```promql
rate(inbox_messages_duplicate_total[5m])
  /
(rate(inbox_messages_registered_total[5m]) + rate(inbox_messages_duplicate_total[5m]))
```

#### Handler duration (p99 latency)

```promql
histogram_quantile(0.99, rate(inbox_handler_duration_milliseconds_bucket[5m]))
```

#### Average handler duration

```promql
rate(inbox_handler_duration_milliseconds_sum[5m])
  /
rate(inbox_handler_duration_milliseconds_count[5m])
```

---

## Registered Services

| Service | Lifetime | Notes |
| --- | --- | --- |
| `IInboxStorage` | Scoped | `InboxStorage<TContext>` — reuses the scoped DbContext |
| `InboxConsumerDecorator<TMessage>` | Scoped | Created per message delivery by `AddInboxConsumer` |
| `TConsumer` | Scoped | Your consumer, resolved inside the decorator |
