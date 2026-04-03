using Inventory.Consumer.Consumers;
using Inventory.Consumer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddMessaging()
    .UseRabbitMq(options =>
        options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!)
    .AddConsumer<OrderCreatedConsumer, OrderCreatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-created";
        config.Queue = "inventory.order-created";
    })
    .AddConsumer<OrderCustomerUpdatedConsumer, OrderCustomerUpdatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-customer-updated";
        config.Queue = "inventory.order-customer-updated";
    })
    .AddConsumer<OrderTotalAmountUpdatedConsumer, OrderTotalAmountUpdatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-total-amount-updated";
        config.Queue = "inventory.order-total-amount-updated";
    });

var host = builder.Build();
host.Run();
