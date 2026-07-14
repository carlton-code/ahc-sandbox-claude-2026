using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AHC.Sandbox.Api.Controllers
{
    /// <summary>
    /// <para>Controller for managing customers and related operations.</para>
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
        {
            var customers = await _customerService.GetCustomersAsync(cancellationToken);

            return Ok(customers);
        }

        [HttpGet("{customerId:int}")]
        public async Task<IActionResult> GetCustomerById(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId, cancellationToken);

            return customer is null ? NotFound() : Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto customer, CancellationToken cancellationToken)
        {
            var createdCustomer = await _customerService.CreateCustomerAsync(customer, cancellationToken);

            return CreatedAtAction(
                nameof(GetCustomerById),
                new { customerId = createdCustomer.CustomerId },
                createdCustomer);
        }

        [HttpPut("{customerId:int}")]
        public async Task<IActionResult> UpdateCustomer(
            int customerId,
            [FromBody] UpdateCustomerDto customer,
            CancellationToken cancellationToken)
        {
            var updated = await _customerService.UpdateCustomerAsync(customerId, customer, cancellationToken);

            return updated ? NoContent() : NotFound();
        }

        [HttpPatch("{customerId:int}")]
        public async Task<IActionResult> PatchCustomer(
            int customerId,
            [FromBody] PatchCustomerDto customer,
            CancellationToken cancellationToken)
        {
            var updatedCustomer = await _customerService.PatchCustomerAsync(customerId, customer, cancellationToken);

            return updatedCustomer is null ? NotFound() : Ok(updatedCustomer);
        }

        [HttpDelete("{customerId:int}")]
        public async Task<IActionResult> DeleteCustomer(int customerId, CancellationToken cancellationToken)
        {
            var deleted = await _customerService.DeleteCustomerAsync(customerId, cancellationToken);

            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("{customerId:int}/orders")]
        public async Task<IActionResult> GetCustomerOrders(int customerId, CancellationToken cancellationToken)
        {
            var orders = await _customerService.GetCustomerOrdersAsync(customerId, cancellationToken);

            return orders is null ? NotFound() : Ok(orders);
        }

        [HttpGet("{customerId:int}/orders/{orderId:int}")]
        public async Task<IActionResult> GetCustomerOrderById(
            int customerId,
            int orderId,
            CancellationToken cancellationToken)
        {
            var order = await _customerService.GetCustomerOrderByIdAsync(customerId, orderId, cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }

        [HttpGet("{customerId:int}/summary")]
        public async Task<IActionResult> GetCustomerSummary(int customerId, CancellationToken cancellationToken)
        {
            var summary = await _customerService.GetCustomerSummaryAsync(customerId, cancellationToken);

            return summary is null ? NotFound() : Ok(summary);
        }

        [HttpGet("{customerId:int}/recent-orders")]
        public async Task<IActionResult> GetCustomerRecentOrders(int customerId, CancellationToken cancellationToken)
        {
            var orders = await _customerService.GetCustomerRecentOrdersAsync(customerId, cancellationToken);

            return orders is null ? NotFound() : Ok(orders);
        }

        [HttpGet("{customerId:int}/order-summary")]
        public async Task<IActionResult> GetCustomerOrderSummary(int customerId, CancellationToken cancellationToken)
        {
            var summary = await _customerService.GetCustomerOrderSummaryAsync(customerId, cancellationToken);

            return summary is null ? NotFound() : Ok(summary);
        }
    }
}
