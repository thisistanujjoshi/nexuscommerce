using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

/// <summary>
/// Aggregate root for the order lifecycle:
/// Pending → Confirmed → Shipped → Delivered, with cancellation
/// allowed only before shipping.
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerEmail { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime PlacedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Total => _items.Sum(i => i.LineTotal);

    // EF Core materialization
    private Order()
    {
        CustomerEmail = null!;
    }

    public Order(Guid customerId, string customerEmail, IEnumerable<OrderItem> items)
    {
        if (customerId == Guid.Empty)
            throw new OrderDomainException("Order requires a customer id.");
        if (string.IsNullOrWhiteSpace(customerEmail) || !customerEmail.Contains('@'))
            throw new OrderDomainException("Order requires a valid customer email.");

        var itemList = items?.ToList() ?? [];
        if (itemList.Count == 0)
            throw new OrderDomainException("Order must contain at least one item.");

        Id = Guid.NewGuid();
        CustomerId = customerId;
        CustomerEmail = customerEmail.Trim();
        Status = OrderStatus.Pending;
        PlacedAtUtc = DateTime.UtcNow;
        _items.AddRange(itemList);
    }

    public void Confirm()
    {
        Transition(from: [OrderStatus.Pending], to: OrderStatus.Confirmed);
    }

    public void Ship()
    {
        Transition(from: [OrderStatus.Confirmed], to: OrderStatus.Shipped);
    }

    public void Deliver()
    {
        Transition(from: [OrderStatus.Shipped], to: OrderStatus.Delivered);
    }

    public void Cancel()
    {
        Transition(from: [OrderStatus.Pending, OrderStatus.Confirmed], to: OrderStatus.Cancelled);
    }

    private void Transition(OrderStatus[] from, OrderStatus to)
    {
        if (!from.Contains(Status))
            throw new OrderDomainException(
                $"Cannot move order from {Status} to {to}.");

        Status = to;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
