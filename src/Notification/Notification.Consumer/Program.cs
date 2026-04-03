using Microsoft.EntityFrameworkCore;

using Notification.Consumer.Consumers;
using Notification.Consumer.Infrastructure;

using Shared.Contracts.Events;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddMessaging()
    .UseRabbitMq(options =>
        options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!)
    .AddConsumer<OrderCreatedConsumer, OrderCreatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-created";
        config.Queue = "notification.order-created";
    })
    .AddConsumer<OrderCustomerUpdatedConsumer, OrderCustomerUpdatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-customer-updated";
        config.Queue = "notification.order-customer-updated";
    })
    .AddConsumer<OrderTotalAmountUpdatedConsumer, OrderTotalAmountUpdatedIntegrationEvent>(config =>
    {
        config.Exchange = "order-total-amount-updated";
        config.Queue = "notification.order-total-amount-updated";
    });

var host = builder.Build();
host.Run();