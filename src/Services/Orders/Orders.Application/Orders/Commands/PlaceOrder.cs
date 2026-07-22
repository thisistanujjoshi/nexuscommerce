using MediatR;
using Orders.Application.Abstractions;
using Orders.Domain.Entities;

namespace Orders.Application.Orders.Commands;

public record PlaceOrderItem(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record PlaceOrderCommand(
    Guid CustomerId,
    string CustomerEmail,
    IReadOnlyList<PlaceOrderItem> Items) : IRequest<OrderDto>;

public class PlaceOrderHandler(IOrdersDbContext context) : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => new OrderItem(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity));

        var order = new Order(request.CustomerId, request.CustomerEmail, items);

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        return OrderDto.FromEntity(order);
    }
}
