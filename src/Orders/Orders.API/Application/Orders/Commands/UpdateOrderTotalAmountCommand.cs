namespace Orders.API.Application.Orders.Commands;

public record UpdateOrderTotalAmountCommand(Guid OrderId, decimal TotalAmount);
