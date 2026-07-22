using Microsoft.EntityFrameworkCore;
using Orders.Application.Common;
using Orders.Application.Orders.Commands;
using Orders.Application.Orders.Queries;
using Orders.Domain;
using Orders.Infrastructure.Persistence;
using Xunit;

namespace Orders.UnitTests.Application;

public class OrderHandlerTests
{
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

        var dto = await new PlaceOrderHandler(context).Handle(ValidCommand(), default);

        Assert.Equal(OrderStatus.Pending, dto.Status);
        Assert.Equal(25m, dto.Total);
        Assert.Single(dto.Items);
        Assert.Equal(1, await context.Orders.CountAsync());
    }

    [Fact]
    public async Task ConfirmOrder_TransitionsStatus()
    {
        await using var context = NewContext();
        var placed = await new PlaceOrderHandler(context).Handle(ValidCommand(), default);

        var confirmed = await new ConfirmOrderHandler(context)
            .Handle(new ConfirmOrderCommand(placed.Id), default);

        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task ConfirmOrder_WhenMissing_ThrowsNotFound()
    {
        await using var context = NewContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ConfirmOrderHandler(context).Handle(new ConfirmOrderCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task GetOrderById_ReturnsOrderWithItems()
    {
        await using var context = NewContext();
        var placed = await new PlaceOrderHandler(context).Handle(ValidCommand(), default);

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
        var handler = new PlaceOrderHandler(context);

        var mine = await handler.Handle(ValidCommand(customer), default);
        await handler.Handle(ValidCommand(), default);

        await new ConfirmOrderHandler(context).Handle(new ConfirmOrderCommand(mine.Id), default);

        var result = await new GetOrdersHandler(context)
            .Handle(new GetOrdersQuery(CustomerId: customer, Status: OrderStatus.Confirmed), default);

        Assert.Single(result.Items);
        Assert.Equal(mine.Id, result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task CancelOrder_AfterShipping_SurfacesDomainError()
    {
        await using var context = NewContext();
        var placed = await new PlaceOrderHandler(context).Handle(ValidCommand(), default);
        await new ConfirmOrderHandler(context).Handle(new ConfirmOrderCommand(placed.Id), default);
        await new ShipOrderHandler(context).Handle(new ShipOrderCommand(placed.Id), default);

        await Assert.ThrowsAsync<Orders.Domain.Exceptions.OrderDomainException>(() =>
            new CancelOrderHandler(context).Handle(new CancelOrderCommand(placed.Id), default));
    }
}
