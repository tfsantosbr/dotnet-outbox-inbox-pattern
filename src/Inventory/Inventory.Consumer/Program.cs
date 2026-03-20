using Inventory.Consumer;
using Inventory.Consumer.Consumers;
using Inventory.Consumer.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddMessagingServices();
builder.Services.AddSingleton<OrderCreatedConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
