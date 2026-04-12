using Inventory.Consumer.Infrastructure;

namespace Inventory.Consumer.Application.Products.Commands;

public class ReduceStockCommandHandler(
    InventoryDbContext dbContext,
    ILogger<ReduceStockCommandHandler> logger)
{
    public async Task HandleAsync(ReduceStockCommand command, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FindAsync([command.ProductId], cancellationToken);

        if (product is null)
        {
            logger.LogWarning(
                "Product {ProductId} not found. Cannot reduce stock by {Quantity}.",
                command.ProductId,
                command.Quantity);

            throw new InvalidOperationException(
                $"Product '{command.ProductId}' was not found in inventory.");
        }

        logger.LogInformation(
            "Reducing stock for Product {ProductId} by {Quantity}. Current stock: {Stock}.",
            product.Id,
            command.Quantity,
            product.Stock);

        product.ReduceStock(command.Quantity);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stock reduced for Product {ProductId}. New stock: {Stock}.",
            product.Id,
            product.Stock);
    }
}
