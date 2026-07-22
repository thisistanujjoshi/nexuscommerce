using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Application.Common;
using Orders.Application.Events;
using Orders.Domain.Entities;

namespace Orders.Application.Orders.Commands;

public record ConfirmOrderCommand(Guid OrderId) : IRequest<OrderDto>;
public record ShipOrderCommand(Guid OrderId) : IRequest<OrderDto>;
public record DeliverOrderCommand(Guid OrderId) : IRequest<OrderDto>;
public record CancelOrderCommand(Guid OrderId) : IRequest<OrderDto>;

/// <summary>
/// Shared base for the four lifecycle transitions — load the aggregate,
/// apply the domain transition, persist, publish the status-change event,
/// return the updated projection.
/// </summary>
public abstract class TransitionOrderHandler<TCommand>(IOrdersDbContext context, IEventPublisher publisher)
    : IRequestHandler<TCommand, OrderDto> where TCommand : IRequest<OrderDto>
{
    protected abstract Guid OrderIdOf(TCommand command);
    protected abstract void Apply(Order order);

    public async Task<OrderDto> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var id = OrderIdOf(request);
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), id);

        var oldStatus = order.Status;
        Apply(order);
        await context.SaveChangesAsync(cancellationToken);

        await publisher.PublishAsync(
            OrderEventTypes.StatusChanged,
            new OrderStatusChangedEvent(
                order.Id, order.CustomerId, order.CustomerEmail,
                oldStatus.ToString(), order.Status.ToString()),
            cancellationToken);

        return OrderDto.FromEntity(order);
    }
}

public class ConfirmOrderHandler(IOrdersDbContext context, IEventPublisher publisher)
    : TransitionOrderHandler<ConfirmOrderCommand>(context, publisher)
{
    protected override Guid OrderIdOf(ConfirmOrderCommand command) => command.OrderId;
    protected override void Apply(Order order) => order.Confirm();
}

public class ShipOrderHandler(IOrdersDbContext context, IEventPublisher publisher)
    : TransitionOrderHandler<ShipOrderCommand>(context, publisher)
{
    protected override Guid OrderIdOf(ShipOrderCommand command) => command.OrderId;
    protected override void Apply(Order order) => order.Ship();
}

public class DeliverOrderHandler(IOrdersDbContext context, IEventPublisher publisher)
    : TransitionOrderHandler<DeliverOrderCommand>(context, publisher)
{
    protected override Guid OrderIdOf(DeliverOrderCommand command) => command.OrderId;
    protected override void Apply(Order order) => order.Deliver();
}

public class CancelOrderHandler(IOrdersDbContext context, IEventPublisher publisher)
    : TransitionOrderHandler<CancelOrderCommand>(context, publisher)
{
    protected override Guid OrderIdOf(CancelOrderCommand command) => command.OrderId;
    protected override void Apply(Order order) => order.Cancel();
}
