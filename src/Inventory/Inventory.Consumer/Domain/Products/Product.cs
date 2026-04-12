namespace Inventory.Consumer.Domain.Products;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Stock { get; private set; }

    private Product()
    {
    }

    public Product(Guid id, string name, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        if (stock < 0)
            throw new ArgumentOutOfRangeException(nameof(stock), "Initial stock cannot be negative.");

        Id = id;
        Name = name;
        Stock = stock;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (quantity > Stock)
            throw new InvalidOperationException($"Insufficient stock. Requested: {quantity}, Available: {Stock}.");

        Stock -= quantity;
    }
}
