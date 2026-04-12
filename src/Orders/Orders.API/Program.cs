using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orders.API.Application.Orders.Commands;
using Orders.API.Endpoints;
using Orders.API.Infrastructure;
using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;
using Shared.Messaging.RabbitMQ.Options;
using Shared.Outbox.Extensions;
using Shared.Outbox.Metrics;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("Database")));

// Messaging

builder.Services
    .AddMessaging()
    .UseRabbitMq(options =>
    {
        options.ConnectionString = configuration.GetConnectionString("RabbitMQ")!;
        options.PublisherConfirmationsEnabled = true;
        options.PublisherConfirmationTrackingEnabled = true;
    })
    .AddPublishOptions<OrderCreatedIntegrationEvent>(o =>
    {
        o.Destination = "order-created";
        o.ExchangeType = RabbitMqExchangeType.Fanout;
    })
    .AddPublishOptions<OrderCustomerUpdatedIntegrationEvent>(o =>
    {
        o.Destination = "order-customer-updated";
        o.ExchangeType = RabbitMqExchangeType.Fanout;
    })
    .AddPublishOptions<OrderTotalAmountUpdatedIntegrationEvent>(o =>
    {
        o.Destination = "order-total-amount-updated";
        o.ExchangeType = RabbitMqExchangeType.Fanout;
    });

// Outbox

builder.Services.AddOutbox<OrdersDbContext>()
    .UsePostgresStorage(o =>
    {
        o.ConnectionString = configuration.GetConnectionString("Database")!;
        o.Schema = "orders";
        o.TableName = "outbox_messages";
    })
    .WithSettings(o =>
    {
        o.IntervalInSeconds = 5;
        o.BatchSize = 1000;
        o.MaxParallelism = 5;
    })
    .WithMetrics();

builder.Services.AddScoped<CreateOrderCommandHandler>();
builder.Services.AddScoped<UpdateOrderCustomerCommandHandler>();
builder.Services.AddScoped<UpdateOrderTotalAmountCommandHandler>();

// Observability

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("orders-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(OutboxInstrumentation.MeterName)
        .AddOtlpExporter());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var seedEnabled = app.Configuration.GetValue<bool>("Seed:Enabled");

    db.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS orders");
    db.Database.Migrate();

    if (seedEnabled && !db.OutboxMessages.Any())
    {
        const int totalMessages = 3_000_000;
        const int batchSize = 1_000_000;
        int totalBatches = totalMessages / batchSize;

        logger.LogInformation("Seeding {TotalMessages} outbox messages in {TotalBatches} batches of {BatchSize}",
            totalMessages, totalBatches, batchSize);

        for (int batch = 0; batch < totalBatches; batch++)
        {
            int offset = batch * batchSize;
            logger.LogInformation("Inserting batch {Batch}/{TotalBatches} (rows {From} to {To})",
                batch + 1, totalBatches, offset + 1, offset + batchSize);

            db.Database.SetCommandTimeout(300);
            db.Database.ExecuteSql($"""
                INSERT INTO "orders"."outbox_messages"
                    ("Id", "Type", "Destination", "Content", "Headers", "OccurredOnUtc")
                SELECT
                    gen_random_uuid(),
                    'Shared.Contracts.Events.OrderCreatedIntegrationEvent, Shared.Contracts',
                    'order-created',
                    json_build_object(
                        'OrderId',     gen_random_uuid(),
                        'CustomerId',  gen_random_uuid(),
                        'TotalAmount', 100.00,
                        'ProductId',   '00000000-0000-0000-0000-000000000001',
                        'Quantity',    1
                    )::jsonb,
                    json_build_object('correlation-id', gen_random_uuid()::text)::jsonb,
                    NOW() + (({offset} + gs) * interval '1 millisecond')
                FROM generate_series(1, {batchSize}) AS gs
                """);

            logger.LogInformation("Batch {Batch}/{TotalBatches} inserted successfully", batch + 1, totalBatches);
        }

        logger.LogInformation("Seed completed: {TotalMessages} outbox messages inserted", totalMessages);
    }
    else
    {
        logger.LogInformation(
            seedEnabled
                ? "Outbox messages already seeded, skipping"
                : "Seed disabled, skipping outbox seed");
    }
}

app.MapOrdersEndpoints();

app.Run();
