namespace Orders.API.Application.Orders;

public record CreateOrderCommand(Guid CustomerId, decimal TotalAmount);
