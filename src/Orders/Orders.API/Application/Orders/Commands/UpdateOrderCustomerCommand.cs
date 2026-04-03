namespace Orders.API.Application.Orders.Commands;

public record UpdateOrderCustomerCommand(Guid OrderId, Guid CustomerId);