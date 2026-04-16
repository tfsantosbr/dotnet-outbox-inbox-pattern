using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orders.API.Application.Orders.Commands;
using Orders.API.Endpoints;
using Orders.API.Infrastructure;
using Orders.API.Infrastructure.Extensions;
using Orders.API.Infrastructure.Seeding;
using Outbox.Abstractions.Extensions;
using Outbox.Abstractions.Metrics;
using Outbox.EntityFrameworkCore.PostgreSQL.Extensions;
using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;
using Shared.Messaging.RabbitMQ.Options;

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
    .UsePostgreSQLStorage<OrdersDbContext>(o =>
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

// Feature Management

builder.Services.AddFeatureManagement();

// Seeders

builder.Services.AddDatabaseSeeder<OutboxStressTestSeeder>();
builder.Services.AddDatabaseSeeder<InboxDuplicateTestSeeder>();

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

app.ApplyMigrations();

await app.RunSeedersAsync();

app.MapOrdersEndpoints();

app.Run();