using Orders.API.Application.Orders.Commands;

namespace Orders.API.Endpoints;

public record UpdateOrderCustomerRequest(Guid CustomerId);
public record UpdateOrderTotalAmountRequest(decimal TotalAmount);

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (
            CreateOrderCommand command,
            CreateOrderCommandHandler handler,
            HttpContext httpContext) =>
        {
            var correlationId = httpContext.Request.Headers["correlation-id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
                return Results.BadRequest(new { error = "correlation-id header is required." });

            httpContext.Response.Headers.Append("correlation-id", correlationId);

            var id = await handler.HandleAsync(command, correlationId);
            return Results.Created($"/orders/{id}", new { id });
        });

        app.MapPut("/orders/{orderId:guid}/customer", async (
            Guid orderId,
            UpdateOrderCustomerRequest body,
            UpdateOrderCustomerCommandHandler handler,
            HttpContext httpContext) =>
        {
            var correlationId = httpContext.Request.Headers["correlation-id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
                return Results.BadRequest(new { error = "correlation-id header is required." });

            httpContext.Response.Headers.Append("correlation-id", correlationId);

            var command = new UpdateOrderCustomerCommand(orderId, body.CustomerId);
            var found = await handler.HandleAsync(command, correlationId);

            return found ? Results.NoContent() : Results.NotFound();
        });

        app.MapPut("/orders/{orderId:guid}/total-amount", async (
            Guid orderId,
            UpdateOrderTotalAmountRequest body,
            UpdateOrderTotalAmountCommandHandler handler,
            HttpContext httpContext) =>
        {
            var correlationId = httpContext.Request.Headers["correlation-id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
                return Results.BadRequest(new { error = "correlation-id header is required." });

            httpContext.Response.Headers.Append("correlation-id", correlationId);

            var command = new UpdateOrderTotalAmountCommand(orderId, body.TotalAmount);
            var found = await handler.HandleAsync(command, correlationId);

            return found ? Results.NoContent() : Results.NotFound();
        });

        return app;
    }
}
