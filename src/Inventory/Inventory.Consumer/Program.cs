using Inventory.Consumer;
using Inventory.Consumer.Consumers;
using Inventory.Consumer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddMessaging().UseRabbitMq(options =>
    options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!);
builder.Services.AddSingleton<OrderCreatedConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
