using InboxPattern.Abstractions.Extensions;
using InboxPattern.Abstractions.Metrics;
using InboxPattern.EntityFrameworkCore.PostgreSQL.Extensions;

using Inventory.Consumer.Application.Products.Commands;
using Inventory.Consumer.Consumers;
using Inventory.Consumer.Domain.Products;
using Inventory.Consumer.Infrastructure;

using Microsoft.EntityFrameworkCore;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddScoped<ReduceStockCommandHandler>();

builder.Services
    .AddInbox()
    .UsePostgreSQLStorage<InventoryDbContext>(options =>
    {
        options.Schema = "inventory";
        options.TableName = "inbox_messages";
    })
    .WithMetrics();

builder.Services.AddMessaging()
    .UseRabbitMq(options =>
        options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!)
    .AddInboxConsumer<OrderCreatedConsumer, OrderCreatedIntegrationEvent, InventoryDbContext>(config =>
    {
        config.Exchange = "order-created";
        config.Queue = "inventory.order-created";
        config.ConsumerName = "inventory.order-created-consumer";
    })
    .AddConsumer<OrderCustomerUpdatedConsumer, OrderCustomerUpdatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-customer-updated";
        config.Queue = "inventory.order-customer-updated";
        config.ConsumerName = "inventory.order-customer-updated-consumer";
    })
    .AddConsumer<OrderTotalAmountUpdatedConsumer, OrderTotalAmountUpdatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-total-amount-updated";
        config.Queue = "inventory.order-total-amount-updated";
        config.ConsumerName = "inventory.order-total-amount-updated-consumer";
    });

// Observability

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("inventory-consumer"))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(InboxInstrumentation.MeterName)
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();

    db.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS inventory");
    db.Database.Migrate();

    var seedEnabled = configuration.GetValue<bool>("Seed:Enabled");

    if (seedEnabled && !db.Products.Any())
    {
        db.Products.Add(new Product(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "Default Product",
            stock: 10_000));
        db.SaveChanges();
    }
}

host.Run();
