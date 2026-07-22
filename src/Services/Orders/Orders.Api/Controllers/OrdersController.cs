using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Orders;
using Orders.Application.Orders.Commands;
using Orders.Application.Orders.Queries;
using Orders.Domain;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Produces("application/json")]
public class OrdersController(ISender mediator) : ControllerBase
{
    /// <summary>List orders with optional customer/status filters and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderDto>>> List(
        [FromQuery] Guid? customerId,
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetOrdersQuery(customerId, status, page, pageSize), ct));
    }

    /// <summary>Get a single order by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetOrderByIdQuery(id), ct));
    }

    /// <summary>Place a new order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Place(PlaceOrderCommand command, CancellationToken ct)
    {
        var created = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Confirm a pending order.</summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Confirm(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ConfirmOrderCommand(id), ct));
    }

    /// <summary>Mark a confirmed order as shipped.</summary>
    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Ship(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new ShipOrderCommand(id), ct));
    }

    /// <summary>Mark a shipped order as delivered.</summary>
    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Deliver(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new DeliverOrderCommand(id), ct));
    }

    /// <summary>Cancel an order (only while pending or confirmed).</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new CancelOrderCommand(id), ct));
    }
}
