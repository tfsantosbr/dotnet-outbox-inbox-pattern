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
| `Shared.Outbox.Abstractions` | `IOutboxPublisher`, `OutboxMessage` |
| `Shared.Outbox` | EF Core config, publisher implementation, background processor, storage |

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

`OutboxMessageEntityConfig` maps the entity to a PostgreSQL table with `jsonb` columns for `Headers` and `Content`, and indexes on `OccurredOn`, `ProcessedOn`, and `ErrorHandledOn`.

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
| `OccurredOn` | `timestamp` | When the event was created |
| `ProcessedOn` | `timestamp?` | When it was published (null = pending) |
| `ErrorHandledOn` | `timestamp?` | When the last error occurred |
| `Error` | `text?` | Last error message |

---

### 3. Register the Outbox Services (`AddOutboxServices`)

In `Program.cs`, call `AddOutboxServices<TDbContext>` **after** registering the messaging infrastructure:

```csharp
using Shared.Outbox.Extensions;

// Messaging must be registered first
builder.Services.AddMessaging().UseRabbitMq(options =>
    options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!);

// Outbox
builder.Services.AddOutboxServices<OrdersDbContext>(
    moduleName: "orders",
    connectionString: builder.Configuration.GetConnectionString("Database")!,
    intervalInSeconds: 10,
    messagesBatchSize: 30,
    tableName: "outbox_messages"   // must match OutboxMessageEntityConfig table name
);
```

#### Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `moduleName` | `string` | Unique name for this module. Used as the DB schema and as the **keyed service key** for `IOutboxPublisher`. |
| `connectionString` | `string` | PostgreSQL connection string. Used directly by Dapper in the background processor (bypasses EF Core). |
| `intervalInSeconds` | `int` | How often the background processor polls for unprocessed messages. |
| `messagesBatchSize` | `int` | Maximum number of messages processed per poll cycle. |
| `tableName` | `string` | Name of the outbox table. Must match what was passed to `OutboxMessageEntityConfig`. Defaults to `"OutboxMessages"`. |

#### What Gets Registered

- **`IOutboxPublisher`** — registered as a **keyed scoped service** under `moduleName`. Inject it using `[FromKeyedServices("moduleName")]`.
- **`OutboxProcessorBackgroundService`** — a `BackgroundService` that polls the outbox table, publishes messages to RabbitMQ via `IMessageBus`, and marks them as processed. Includes an exponential back-off retry policy (up to 5 attempts with jitter via Polly).

---

### 4. Publish Events via `IOutboxPublisher`

Inject `IOutboxPublisher` using the keyed service attribute matching the `moduleName` used in registration:

```csharp
using Shared.Outbox.Abstractions;

public class CreateOrderCommandHandler(
    OrdersDbContext dbContext,
    [FromKeyedServices("orders")] IOutboxPublisher outboxPublisher)
{
    public async Task<Guid> HandleAsync(CreateOrderCommand command, string correlationId)
    {
        var order = new Order(
            Guid.CreateVersion7(),
            command.CustomerId,
            DateTime.UtcNow,
            command.TotalAmount);

        dbContext.Orders.Add(order);

        var @event = new OrderCreatedIntegrationEvent(
            orderId: order.Id,
            customerId: order.CustomerId,
            totalAmount: order.TotalAmount,
            occurredOnUtc: order.CreatedOnUtc,
            correlationId: correlationId,
            causationId: order.Id.ToString(),
            source: "Orders.API");

        // Optional: propagate headers (e.g. correlation ID for distributed tracing)
        var headers = new Dictionary<string, string> { { "X-Correlation-Id", correlationId } };

        // Stages the event — does NOT publish to RabbitMQ yet
        await outboxPublisher.Publish(@event, "order-created", headers);

        // Saves both the Order and the OutboxMessage in a single transaction
        await dbContext.SaveChangesAsync();

        return order.Id;
    }
}
```

`IOutboxPublisher.Publish` only adds the `OutboxMessage` row to the EF Core change tracker. The actual publish to the broker happens **after** `SaveChangesAsync` completes, when `OutboxProcessorBackgroundService` picks it up.

#### `IOutboxPublisher` signature

```csharp
public interface IOutboxPublisher
{
    Task Publish<TEvent>(
        TEvent integrationEvent,
        string destination,
        IDictionary<string, string>? headers = null
    ) where TEvent : IEventBase;
}
```

| Parameter | Description |
| --- | --- |
| `integrationEvent` | The event to publish. Must implement `IEventBase`. |
| `destination` | Queue or exchange name on RabbitMQ. |
| `headers` | Optional metadata (correlation IDs, tracing, etc.). Stored as `jsonb` and forwarded to the broker. |

---

## Background Processor Behavior

`OutboxProcessorBackgroundService` runs in a loop:

1. Opens a PostgreSQL connection and begins a transaction.
2. Selects up to `messagesBatchSize` unprocessed rows (`ProcessedOn IS NULL`), ordered by `OccurredOn`, using `FOR UPDATE` to prevent concurrent processing.
3. For each message, calls `IMessageBus.Publish` with the serialized payload, destination, and headers.
4. On success — sets `ProcessedOn = UTC now`, clears `Error` and `ErrorHandledOn`.
5. On failure — sets `ProcessedOn = UTC now`, `ErrorHandledOn = UTC now`, and stores the exception message in `Error`. Retries up to **5 times** with exponential back-off and jitter (Polly).
6. Commits the transaction and waits `intervalInSeconds` before the next cycle.

---

## Checklist

- [ ] `DbContext` implements `IOutboxDbContext`
- [ ] `DbSet<OutboxMessage> OutboxMessages` declared on the `DbContext`
- [ ] `OutboxMessageEntityConfig` applied in `OnModelCreating` with the correct table name
- [ ] Migration generated and applied
- [ ] `AddMessaging().UseRabbitMq(...)` registered before `AddOutboxServices`
- [ ] `AddOutboxServices<TDbContext>` called with `moduleName`, connection string, interval, batch size, and table name
- [ ] `IOutboxPublisher` injected with `[FromKeyedServices("moduleName")]`
- [ ] `outboxPublisher.Publish(...)` called **before** `SaveChangesAsync`
