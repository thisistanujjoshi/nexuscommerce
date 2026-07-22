using Orders.Domain;
using Orders.Domain.Entities;

namespace Orders.Application.Orders;

public record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal)
{
    public static OrderItemDto FromEntity(OrderItem item) =>
        new(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.LineTotal);
}

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    OrderStatus Status,
    decimal Total,
    DateTime PlacedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<OrderItemDto> Items)
{
    public static OrderDto FromEntity(Order order) => new(
        order.Id,
        order.CustomerId,
        order.CustomerEmail,
        order.Status,
        order.Total,
        order.PlacedAtUtc,
        order.UpdatedAtUtc,
        order.Items.Select(OrderItemDto.FromEntity).ToList());
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
