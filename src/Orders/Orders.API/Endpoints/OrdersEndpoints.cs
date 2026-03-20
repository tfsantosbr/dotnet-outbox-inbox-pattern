using Orders.API.Application.Orders.Commands;

namespace Orders.API.Endpoints;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (CreateOrderCommand command, CreateOrderCommandHandler handler) =>
        {
            var id = await handler.HandleAsync(command);
            return Results.Created($"/orders/{id}", new { id });
        });

        return app;
    }
}
