using Orders.API.Application.Orders.Commands;

namespace Orders.API.Endpoints;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (
            CreateOrderCommand command,
            CreateOrderCommandHandler handler,
            HttpContext httpContext) =>
        {
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
                return Results.BadRequest(new { error = "X-Correlation-Id header is required." });

            httpContext.Response.Headers.Append("X-Correlation-Id", correlationId);

            var id = await handler.HandleAsync(command, correlationId);
            return Results.Created($"/orders/{id}", new { id });
        });

        return app;
    }
}
