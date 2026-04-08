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
        options.ConnectionString = configuration.GetConnectionString("RabbitMQ")!)
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
        o.IntervalInSeconds = 10;
        o.BatchSize = 30;
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
    db.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS orders");
    db.Database.Migrate();
}

app.MapOrdersEndpoints();

app.Run();