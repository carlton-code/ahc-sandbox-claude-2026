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
    /// Customer-scoped order views stay on <see cref="CustomersController"/>
    /// (<c>/customers/{customerId}/orders</c>) — there's no <c>?customerId=</c> filter here.
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
        public async Task<ActionResult<IReadOnlyCollection<OrderDto>>> GetOrders(CancellationToken cancellationToken)
        {
            var orders = await _orderService.GetOrdersAsync(cancellationToken);

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
