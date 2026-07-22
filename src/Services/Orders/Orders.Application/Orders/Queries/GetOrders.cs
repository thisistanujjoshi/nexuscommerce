using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Domain;

namespace Orders.Application.Orders.Queries;

public record GetOrdersQuery(
    Guid? CustomerId = null,
    OrderStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<OrderDto>>;

public class GetOrdersHandler(IOrdersDbContext context) : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = context.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        var total = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.PlacedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderDto>(
            orders.Select(OrderDto.FromEntity).ToList(), page, pageSize, total);
    }
}
