# Outbox

Reliable event publishing using the Transactional Outbox Pattern. Events are stored in an outbox table within the **same database transaction** as domain changes, then published to the message bus by a background service. This guarantees at-least-once delivery even if the broker is temporarily unavailable.

## How It Works

```text
Command Handler
    │
    ├─ Save domain entity  ──┐
    └─ Save OutboxMessage  ──┴─ Single transaction (SaveChangesAsync)
                                        │
                              OutboxProcessorBackgroundService
                                        │
                                  Reads unprocessed rows
                                        │
                                  Publishes to RabbitMQ
                                        │
                                  Marks as processed
```

## Project Structure

| Project | Responsibility |
| --- | --- |
| `Shared.Outbox.Abstractions` | `IOutboxPublisher`, `IOutboxStorage`, `IOutboxDbContext`, `OutboxMessage` |
| `Shared.Outbox` | EF Core config, publisher implementation, background processor, storage, metrics |

---

## Step-by-Step Integration Guide

### 1. Implement `IOutboxDbContext` on your DbContext

Your `DbContext` must implement `IOutboxDbContext` to expose the `OutboxMessages` table.

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.Outbox.Abstractions;
using Shared.Outbox.Database;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    public DbSet<Order> Orders { get; init; }
    public DbSet<OutboxMessage> OutboxMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

        // Register the outbox table configuration.
        // Optionally pass a custom table name (default: "OutboxMessages").
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfig("outbox_messages"));
    }
}
```

`IOutboxDbContext` interface:

```csharp
public interface IOutboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
}
```

`OutboxMessageEntityConfig` maps the entity to a PostgreSQL table with `jsonb` columns for `Headers` and `Content`, and indexes on `OccurredOnUtc`, `ProcessedOnUtc`, and `ErrorHandledOnUtc`.

---

### 2. Add the EF Core Migration

After configuring the `DbContext`, generate the migration to create the outbox table:

```bash
dotnet ef migrations add AddOutboxMessageTable \
  --project src/Orders/Orders.API \
  --context OrdersDbContext
```

The migration will create a table with the following columns:

| Column | Type | Description |
| --- | --- | --- |
| `Id` | `uuid` | Event ID (never auto-generated) |
| `Type` | `varchar(500)` | Assembly-qualified type name |
| `Destination` | `text` | Target queue / exchange name |
| `Content` | `jsonb` | Serialized event payload |
| `Headers` | `jsonb` | Optional headers (e.g. correlation IDs) |
| `OccurredOnUtc` | `timestamp` | When the event was created |
| `ProcessedOnUtc` | `timestamp?` | When it was published (`null` = pending) |
| `ErrorHandledOnUtc` | `timestamp?` | When the last error occurred |
| `Error` | `text?` | Last error message |

---

### 3. Register the Outbox Services

In `Program.cs`, call `AddOutbox<TDbContext>` (or `AddKeyedOutbox<TDbContext>`) **after** registering the messaging infrastructure. The method returns an `OutboxBuilder` for fluent configuration.

#### Non-keyed (single outbox per service)

Use when the service has a single database context and a single outbox.

```csharp
using Shared.Outbox.Extensions;

// Messaging must be registered first
builder.Services
    .AddMessaging()
    .UseRabbitMq(o => o.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!)
    .AddPublishOptions<OrderCreatedIntegrationEvent>(o =>
    {
        o.Destination  = "orders";
        o.ExchangeType = RabbitMqExchangeType.Fanout;
        o.Durable      = true;
    });

// Outbox
builder.Services.AddOutbox<OrdersDbContext>()
    .UsePostgresStorage(o =>
    {
        o.ConnectionString = builder.Configuration.GetConnectionString("Database")!;
        o.Schema           = "orders";    // default: "public"
        o.TableName        = "outbox_messages";  // must match OutboxMessageEntityConfig
    })
    .WithSettings(o =>
    {
        o.IntervalInSeconds = 10;  // default: 10
        o.BatchSize         = 30;  // default: 30
    });
```

`IOutboxPublisher` is registered as a **plain scoped service** and can be injected directly.

#### Keyed (multiple outboxes in the same service)

Use when a single host runs multiple modules, each with its own `DbContext`.

```csharp
builder.Services.AddKeyedOutbox<OrdersDbContext>("orders")
    .UsePostgresStorage(o => { ... })
    .WithSettings(o => { ... });

builder.Services.AddKeyedOutbox<InventoryDbContext>("inventory")
    .UsePostgresStorage(o => { ... })
    .WithSettings(o => { ... });
```

`IOutboxPublisher` is registered as a **keyed scoped service** under `moduleName`. Inject it using `[FromKeyedServices("moduleName")]`.

#### `OutboxBuilder` options

| Method | Description |
| --- | --- |
| `.UsePostgresStorage(configure)` | Sets `ConnectionString`, `Schema`, and `TableName` |
| `.WithSettings(configure)` | Sets `IntervalInSeconds` and `BatchSize` |
| `.WithResilience(pipeline)` | Replaces the default Polly retry policy |
| `.WithMetrics(configure?)` | Opt-in throughput metrics (see [Metrics](#metrics)) |

#### What Gets Registered

| Service | Lifetime | Notes |
| --- | --- | --- |
| `IOutboxPublisher` | Scoped | Stages messages in the EF Core change tracker |
| `IOutboxStorage` | Transient | Dapper-based read/update via direct connection |
| `OutboxProcessorBackgroundService` | Hosted | Polls and publishes pending messages |

---

### 4. Publish Events via `IOutboxPublisher`

#### Non-keyed injection

```csharp
public class CreateOrderCommandHandler(
    OrdersDbContext dbContext,
    IOutboxPublisher outboxPublisher)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = new Order(Guid.CreateVersion7(), command.CustomerId, DateTime.UtcNow, command.TotalAmount);
        dbContext.Orders.Add(order);

        var @event = new OrderCreatedIntegrationEvent(
            orderId:       order.Id,
            customerId:    order.CustomerId,
            totalAmount:   order.TotalAmount,
            occurredOnUtc: order.CreatedOnUtc);

        var headers = new Dictionary<string, string>
        {
            [MessageHeaders.CorrelationId] = correlationId,
            [MessageHeaders.CausationId]   = order.Id.ToString(),
            [MessageHeaders.Source]        = "Orders.API",
        };

        // Stages the event — does NOT publish to RabbitMQ yet
        await outboxPublisher.PublishAsync(@event, headers, ct);

        // Saves both the Order and the OutboxMessage in a single transaction
        await dbContext.SaveChangesAsync(ct);

        return order.Id;
    }
}
```

#### Keyed injection

```csharp
public class CreateOrderCommandHandler(
    OrdersDbContext dbContext,
    [FromKeyedServices("orders")] IOutboxPublisher outboxPublisher)
{
    // same as above
}
```

#### `IOutboxPublisher` signature

```csharp
public interface IOutboxPublisher
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TEvent : IEventBase;
}
```

| Parameter | Description |
| --- | --- |
| `integrationEvent` | The event to publish. Must implement `IEventBase`. |
| `headers` | Optional metadata forwarded to the broker as `jsonb`. |

> The destination exchange is resolved automatically from `IPublishTopologyRegistry` based on the event type. Register it with `.AddPublishOptions<TEvent>` on the messaging builder.

`PublishAsync` only adds the `OutboxMessage` row to the EF Core change tracker. The actual publish to the broker happens **after** `SaveChangesAsync` completes, when the background processor picks it up.

---

## Background Processor Behavior

`OutboxProcessorBackgroundService` runs in a loop:

1. Opens a PostgreSQL connection and begins a transaction.
2. Selects up to `BatchSize` rows where `ProcessedOnUtc IS NULL`, ordered by `OccurredOnUtc`, using `FOR UPDATE` to prevent concurrent processing.
3. For each message, calls `IMessageBus.PublishAsync` with the serialized payload, destination, and headers.
4. On **success** — sets `ProcessedOnUtc = UTC now`, clears `Error` and `ErrorHandledOnUtc`.
5. On **failure** — sets `ProcessedOnUtc = UTC now`, `ErrorHandledOnUtc = UTC now`, stores the exception message in `Error`. Retries up to **5 times** with exponential back-off and jitter (Polly).
6. Commits the transaction and waits `IntervalInSeconds` before the next cycle.

### Custom resilience policy

Replace the default Polly retry with your own pipeline:

```csharp
using Polly;

var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
    .AddTimeout(TimeSpan.FromSeconds(5))
    .Build();

builder.Services.AddOutbox<OrdersDbContext>()
    .UsePostgresStorage(...)
    .WithResilience(pipeline);
```

---

## Metrics

The outbox exposes opt-in throughput metrics via `System.Diagnostics.Metrics` (BCL). No OpenTelemetry dependency is added to the outbox library — the consuming application decides which observability stack subscribes to the meter.

### Enabling metrics

Call `.WithMetrics()` on the outbox builder in `Program.cs`:

```csharp
builder.Services.AddOutbox<OrdersDbContext>()
    .UsePostgresStorage(...)
    .WithSettings(...)
    .WithMetrics();
```

To add extra global tags applied to every measurement:

```csharp
.WithMetrics(o =>
{
    o.Tags = new Dictionary<string, string>
    {
        ["environment"] = "production"
    };
});
```

### Subscribing the meter (OpenTelemetry)

Add the meter name to the OpenTelemetry metrics configuration in your service:

```csharp
using Shared.Outbox.Metrics;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(OutboxInstrumentation.MeterName));  // "Shared.Outbox"
```

### Available instruments

| Instrument | Type | Unit | Description |
| --- | --- | --- | --- |
| `outbox.messages.published` | Counter | `{message}` | Messages successfully published to the broker |
| `outbox.messages.failed` | Counter | `{message}` | Messages that failed to publish |
| `outbox.messages.processed` | Counter | `{message}` | Total messages processed (success + failure) |
| `outbox.fetch.duration` | Histogram | `ms` | Time taken to fetch a batch of messages from the database |
| `outbox.update.duration` | Histogram | `ms` | Time taken to update a single message in the database |

All instruments include a `module` tag with the value passed to `AddKeyedOutbox("module-name")` when using the keyed variant.

### Grafana — useful queries

#### Throughput (messages/second)

```promql
rate(outbox_messages_published_total[1m])
```

#### Error rate

```promql
rate(outbox_messages_failed_total[1m])
  /
rate(outbox_messages_processed_total[1m])
```

#### Total processed messages (cumulative)

```promql
outbox_messages_processed_total
```

#### Comparing success vs failure over time

```promql
rate(outbox_messages_published_total[5m])
rate(outbox_messages_failed_total[5m])
```

**Filter by module** (when multiple modules are active in the same service):

```promql
rate(outbox_messages_published_total{module="orders"}[1m])
```

#### Fetch duration (p99 latency)

```promql
histogram_quantile(0.99, rate(outbox_fetch_duration_milliseconds_bucket[5m]))
```

#### Average fetch duration

```promql
rate(outbox_fetch_duration_milliseconds_sum[5m])
  /
rate(outbox_fetch_duration_milliseconds_count[5m])
```

#### Update duration (p99 latency)

```promql
histogram_quantile(0.99, rate(outbox_update_duration_milliseconds_bucket[5m]))
```

#### Average update duration

```promql
rate(outbox_update_duration_milliseconds_sum[5m])
  /
rate(outbox_update_duration_milliseconds_count[5m])
```
