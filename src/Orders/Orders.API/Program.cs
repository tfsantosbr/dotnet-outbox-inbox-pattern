using Microsoft.EntityFrameworkCore;
using Orders.API.Application.Orders.Commands;
using Orders.API.Endpoints;
using Orders.API.Infrastructure;
using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;
using Shared.Messaging.RabbitMQ.Options;
using Shared.Outbox.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Messaging

builder.Services
    .AddMessaging()
    .UseRabbitMq(options =>
        options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!)
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

// builder.Services.AddOutboxServices<OrdersDbContext>(
//     moduleName: "orders",
//     connectionString: builder.Configuration.GetConnectionString("Database")!,
//     intervalInSeconds: 10,
//     messagesBatchSize: 30,
//     tableName: "outbox_messages"
// );

builder.Services.AddScoped<CreateOrderCommandHandler>();
builder.Services.AddScoped<UpdateOrderCustomerCommandHandler>();
builder.Services.AddScoped<UpdateOrderTotalAmountCommandHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

app.MapOrdersEndpoints();

app.Run();
