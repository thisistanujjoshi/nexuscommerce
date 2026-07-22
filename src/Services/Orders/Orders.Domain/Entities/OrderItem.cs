using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    // EF Core materialization
    private OrderItem()
    {
        ProductName = null!;
    }

    public OrderItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            throw new OrderDomainException("Order item requires a product id.");
        if (string.IsNullOrWhiteSpace(productName))
            throw new OrderDomainException("Order item requires a product name.");
        if (unitPrice < 0)
            throw new OrderDomainException("Unit price cannot be negative.");
        if (quantity <= 0)
            throw new OrderDomainException("Quantity must be at least 1.");

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
