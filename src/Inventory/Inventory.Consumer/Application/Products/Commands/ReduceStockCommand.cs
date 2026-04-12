namespace Inventory.Consumer.Application.Products.Commands;

public record ReduceStockCommand(Guid ProductId, int Quantity);
