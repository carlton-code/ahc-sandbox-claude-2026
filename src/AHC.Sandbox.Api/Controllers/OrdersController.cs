using AHC.Sandbox.Application.Orders.Dtos;
using AHC.Sandbox.Application.Orders.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AHC.Sandbox.Api.Controllers
{
    /// <summary>
    /// <para>Controller for reading orders.</para>
    /// <para>
    /// Read-only by design: order creation is a real business workflow (a header plus its lines,
    /// money math, status transitions, a database-computed order number), so write support waits
    /// for a genuine use case and the Domain rules to go with it rather than shipping a bare
    /// row-editor. The list returns headers only; the by-id read carries the lines.
    /// </para>
    /// <para>
    /// This is the only place to read orders: the list takes an optional <c>?customerId=</c>
    /// filter, which replaced the old <c>/customers/{customerId}/orders</c> sub-resource. Filter
    /// semantics apply — an unknown customer (or one with no orders) is <c>200</c> with an empty
    /// array, not <c>404</c>. The customer-centric aggregates
    /// (<c>/customers/{customerId}/order-summary</c>, <c>/summary</c>) stay on
    /// <see cref="CustomersController"/>.
    /// </para>
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyCollection<OrderDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<OrderDto>>> GetOrders(
            [FromQuery] int? customerId,
            CancellationToken cancellationToken)
        {
            var orders = await _orderService.GetOrdersAsync(customerId, cancellationToken);

            return Ok(orders);
        }

        [HttpGet("{orderId:int}")]
        [ProducesResponseType<OrderWithLinesDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderWithLinesDto>> GetOrderById(int orderId, CancellationToken cancellationToken)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId, cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }
    }
}
