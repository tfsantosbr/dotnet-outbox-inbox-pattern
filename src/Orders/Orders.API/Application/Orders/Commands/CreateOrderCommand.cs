namespace Orders.API.Application.Orders.Commands;

public record CreateOrderCommand(Guid CustomerId, decimal TotalAmount);
