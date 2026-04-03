namespace Orders.API.Application.Orders;

public class Order(Guid id, Guid customerId, DateTime createdOnUtc, decimal totalAmount)
{
    public Guid Id { get; private set; } = id;
    public Guid CustomerId { get; private set; } = customerId;
    public DateTime CreatedOnUtc { get; private set; } = createdOnUtc;
    public decimal TotalAmount { get; private set; } = totalAmount;

    public void UpdateCustomer(Guid customerId) => CustomerId = customerId;

    public void UpdateTotalAmount(decimal totalAmount) => TotalAmount = totalAmount;
}