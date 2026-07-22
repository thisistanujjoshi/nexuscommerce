using Orders.Domain.Entities;

namespace Orders.Application.Events;

public static class OrderEventTypes
{
    public const string Placed = "order.placed";
    public const string StatusChanged = "order.status-changed";
}

public record OrderPlacedEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    decimal Total,
    IReadOnlyList<OrderPlacedEventItem> Items)
{
    public static OrderPlacedEvent FromEntity(Order order) => new(
        order.Id,
        order.CustomerId,
        order.CustomerEmail,
        order.Total,
        order.Items.Select(i => new OrderPlacedEventItem(i.ProductName, i.Quantity)).ToList());
}

public record OrderPlacedEventItem(string ProductName, int Quantity);

public record OrderStatusChangedEvent(
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string OldStatus,
    string NewStatus);
