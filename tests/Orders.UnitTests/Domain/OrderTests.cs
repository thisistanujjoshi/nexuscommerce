using Orders.Domain;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;
using Xunit;

namespace Orders.UnitTests.Domain;

public class OrderTests
{
    private static OrderItem Item(decimal price = 10m, int qty = 1) =>
        new(Guid.NewGuid(), "Widget", price, qty);

    private static Order NewOrder(params OrderItem[] items) =>
        new(Guid.NewGuid(), "buyer@example.com", items.Length > 0 ? items : [Item()]);

    [Fact]
    public void NewOrder_StartsPending_WithComputedTotal()
    {
        var order = NewOrder(Item(price: 10m, qty: 2), Item(price: 5.5m, qty: 1));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(25.5m, order.Total);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public void NewOrder_WithoutItems_Throws()
    {
        Assert.Throws<OrderDomainException>(() =>
            new Order(Guid.NewGuid(), "buyer@example.com", []));
    }

    [Fact]
    public void NewOrder_WithInvalidEmail_Throws()
    {
        Assert.Throws<OrderDomainException>(() =>
            new Order(Guid.NewGuid(), "not-an-email", [Item()]));
    }

    [Fact]
    public void OrderItem_WithZeroQuantity_Throws()
    {
        Assert.Throws<OrderDomainException>(() =>
            new OrderItem(Guid.NewGuid(), "Widget", 10m, 0));
    }

    [Fact]
    public void HappyPath_PendingToDelivered()
    {
        var order = NewOrder();

        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        order.Ship();
        Assert.Equal(OrderStatus.Shipped, order.Status);

        order.Deliver();
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Cancel_AllowedWhilePending()
    {
        var order = NewOrder();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_AllowedWhileConfirmed()
    {
        var order = NewOrder();
        order.Confirm();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_AfterShipping_Throws()
    {
        var order = NewOrder();
        order.Confirm();
        order.Ship();

        Assert.Throws<OrderDomainException>(order.Cancel);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Ship_WithoutConfirm_Throws()
    {
        var order = NewOrder();

        Assert.Throws<OrderDomainException>(order.Ship);
    }

    [Fact]
    public void Confirm_Twice_Throws()
    {
        var order = NewOrder();
        order.Confirm();

        Assert.Throws<OrderDomainException>(order.Confirm);
    }
}
