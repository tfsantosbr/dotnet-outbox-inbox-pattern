using Microsoft.EntityFrameworkCore;

using Orders.API.Application.Orders.Commands;
using Orders.API.Endpoints;
using Orders.API.Infrastructure;
using Orders.API.Infrastructure.Messaging;


using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = builder.Configuration.GetSection("RabbitMQ");
    var factory = new ConnectionFactory
    {
        HostName = config["Host"] ?? "localhost",
        UserName = config["Username"] ?? "guest",
        Password = config["Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddScoped<CreateOrderCommandHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

app.MapOrdersEndpoints();

app.Run();
