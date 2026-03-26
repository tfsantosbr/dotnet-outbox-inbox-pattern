using Microsoft.EntityFrameworkCore;
using Orders.API.Application.Orders.Commands;
using Orders.API.Endpoints;
using Orders.API.Infrastructure;
using Shared.Messaging.Abstractions.Extensions;
using Shared.Messaging.RabbitMQ.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddMessaging().UseRabbitMq(options =>
    options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")!);
builder.Services.AddScoped<CreateOrderCommandHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

app.MapOrdersEndpoints();

app.Run();
