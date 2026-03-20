using Microsoft.EntityFrameworkCore;
using Notification.Consumer;
using Notification.Consumer.Consumers;
using Notification.Consumer.Infrastructure;
using Shared.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddMessagingServices(builder.Configuration);
builder.Services.AddSingleton<OrderCreatedConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
