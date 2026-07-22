using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Application.Common;
using Orders.Domain.Entities;

namespace Orders.Application.Orders.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

public class GetOrderByIdHandler(IOrdersDbContext context) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        return OrderDto.FromEntity(order);
    }
}
