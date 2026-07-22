using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Application.Common;
using Orders.Application.Events;
using Orders.Application.Orders.Commands;
using Orders.Application.Orders.Queries;
using Orders.Domain;
using Orders.Infrastructure.Persistence;
using Xunit;

namespace Orders.UnitTests.Application;

/// <summary>Test double that records every published event.</summary>
public class RecordingEventPublisher : IEventPublisher
{
    public List<(string EventType, object Data)> Published { get; } = [];

    public Task PublishAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        Published.Add((eventType, data));
        return Task.CompletedTask;
    }
}

public class OrderHandlerTests
{
    private readonly RecordingEventPublisher _publisher = new();

    private static OrdersDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase($"orders-{Guid.NewGuid()}")
            .Options;
        return new OrdersDbContext(options);
    }

    private static PlaceOrderCommand ValidCommand(Guid? customerId = null) => new(
        customerId ?? Guid.NewGuid(),
        "buyer@example.com",
        [new PlaceOrderItem(Guid.NewGuid(), "Widget", 12.5m, 2)]);

    [Fact]
    public async Task PlaceOrder_PersistsAndReturnsDto()
    {
        await using var context = NewContext();

        var dto = await new PlaceOrderHandler(context, _publisher).Handle(ValidCommand(), default);

        Assert.Equal(OrderStatus.Pending, dto.Status);
        Assert.Equal(25m, dto.Total);
        Assert.Single(dto.Items);
        Assert.Equal(1, await context.Orders.CountAsync());
    }

    [Fact]
    public async Task PlaceOrder_PublishesOrderPlacedEvent()
    {
        await using var context = NewContext();

        var dto = await new PlaceOrderHandler(context, _publisher).Handle(ValidCommand(), default);

        var (eventType, data) = Assert.Single(_publisher.Published);
        Assert.Equal(OrderEventTypes.Placed, eventType);
        var placed = Assert.IsType<OrderPlacedEvent>(data);
        Assert.Equal(dto.Id, placed.OrderId);
        Assert.Equal("buyer@example.com", placed.CustomerEmail);
        Assert.Equal(25m, placed.Total);
    }

    [Fact]
    public async Task ConfirmOrder_TransitionsStatus_AndPublishesStatusChange()
    {
        await using var context = NewContext();
        var placed = await new PlaceOrderHandler(context, _publisher).Handle(ValidCommand(), default);

        var confirmed = await new ConfirmOrderHandler(context, _publisher)
            .Handle(new ConfirmOrderCommand(placed.Id), default);

        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);

        var statusEvent = Assert.IsType<OrderStatusChangedEvent>(
            _publisher.Published.Single(p => p.EventType == OrderEventTypes.StatusChanged).Data);
        Assert.Equal("Pending", statusEvent.OldStatus);
        Assert.Equal("Confirmed", statusEvent.NewStatus);
    }

    [Fact]
    public async Task ConfirmOrder_WhenMissing_ThrowsNotFound()
    {
        await using var context = NewContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ConfirmOrderHandler(context, _publisher)
                .Handle(new ConfirmOrderCommand(Guid.NewGuid()), default));
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task GetOrderById_ReturnsOrderWithItems()
    {
        await using var context = NewContext();
        var placed = await new PlaceOrderHandler(context, _publisher).Handle(ValidCommand(), default);

        var fetched = await new GetOrderByIdHandler(context)
            .Handle(new GetOrderByIdQuery(placed.Id), default);

        Assert.Equal(placed.Id, fetched.Id);
        Assert.Single(fetched.Items);
        Assert.Equal("Widget", fetched.Items[0].ProductName);
    }

    [Fact]
    public async Task GetOrders_FiltersByCustomerAndStatus()
    {
        await using var context = NewContext();
        var customer = Guid.NewGuid();
        var handler = new PlaceOrderHandler(context, _publisher);

        var mine = await handler.Handle(ValidCommand(customer), default);
        await handler.Handle(ValidCommand(), default);

        await new ConfirmOrderHandler(context, _publisher).Handle(new ConfirmOrderCommand(mine.Id), default);

        var result = await new GetOrdersHandler(context)
            .Handle(new GetOrdersQuery(CustomerId: customer, Status: OrderStatus.Confirmed), default);

        Assert.Single(result.Items);
        Assert.Equal(mine.Id, result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task CancelOrder_AfterShipping_SurfacesDomainError_AndPublishesNothing()
    {
        await using var context = NewContext();
        var placed = await new PlaceOrderHandler(context, _publisher).Handle(ValidCommand(), default);
        await new ConfirmOrderHandler(context, _publisher).Handle(new ConfirmOrderCommand(placed.Id), default);
        await new ShipOrderHandler(context, _publisher).Handle(new ShipOrderCommand(placed.Id), default);

        var eventsBefore = _publisher.Published.Count;

        await Assert.ThrowsAsync<Orders.Domain.Exceptions.OrderDomainException>(() =>
            new CancelOrderHandler(context, _publisher).Handle(new CancelOrderCommand(placed.Id), default));

        Assert.Equal(eventsBefore, _publisher.Published.Count);
    }
}
