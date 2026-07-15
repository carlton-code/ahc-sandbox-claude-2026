using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Interfaces;
using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Api.Controllers
{
    /// <summary>
    /// <para>Controller for managing customers and related operations.</para>
    /// <para>
    /// Actions return <c>ActionResult&lt;T&gt;</c> so the compiler checks the success type against
    /// the method body, and declare <em>every</em> status they can return — including the success
    /// one — with <c>[ProducesResponseType]</c>.
    /// </para>
    /// <para>
    /// Declaring the success status explicitly is not redundant: adding any
    /// <c>[ProducesResponseType]</c> to an action replaces the framework's inferred 200 rather
    /// than adding to it, so annotating only a 404 would silently drop the 200 (and its schema)
    /// from the OpenAPI document. Being exhaustive on every action keeps that failure mode from
    /// reappearing the next time someone adds an attribute.
    /// </para>
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IAddressService _addressService;

        public CustomersController(ICustomerService customerService, IAddressService addressService)
        {
            _customerService = customerService;
            _addressService = addressService;
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyCollection<CustomerDto>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyCollection<CustomerDto>>> GetCustomers(CancellationToken cancellationToken)
        {
            var customers = await _customerService.GetCustomersAsync(cancellationToken);

            return Ok(customers);
        }

        [HttpGet("{customerId:int}")]
        [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerDto>> GetCustomerById(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId, cancellationToken);

            return customer is null ? NotFound() : Ok(customer);
        }

        /// <summary>
        /// Finds customers by first name, last name, or both separated by a space.
        /// </summary>
        /// <param name="q">
        /// Required. Matched as a case-insensitive substring. <c>[Required]</c> covers missing,
        /// empty, and whitespace-only values on its own — the model binder converts a
        /// whitespace-only query value to null before validation runs — so <c>[ApiController]</c>
        /// returns a 400 before this action body executes. No hand-rolled guard is needed; one
        /// would be unreachable.
        /// </param>
        [HttpGet("search")]
        [ProducesResponseType<IReadOnlyCollection<CustomerDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyCollection<CustomerDto>>> SearchCustomers(
            [FromQuery][Required] string q,
            CancellationToken cancellationToken)
        {
            var customers = await _customerService.SearchCustomersAsync(q, cancellationToken);

            return Ok(customers);
        }

        [HttpPost]
        [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
        public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto customer, CancellationToken cancellationToken)
        {
            var createdCustomer = await _customerService.CreateCustomerAsync(customer, cancellationToken);

            return CreatedAtAction(
                nameof(GetCustomerById),
                new { customerId = createdCustomer.CustomerId },
                createdCustomer);
        }

        [HttpPut("{customerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCustomer(
            int customerId,
            [FromBody] UpdateCustomerDto customer,
            CancellationToken cancellationToken)
        {
            var updated = await _customerService.UpdateCustomerAsync(customerId, customer, cancellationToken);

            return updated ? NoContent() : NotFound();
        }

        [HttpPatch("{customerId:int}")]
        [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerDto>> PatchCustomer(
            int customerId,
            [FromBody] PatchCustomerDto customer,
            CancellationToken cancellationToken)
        {
            var updatedCustomer = await _customerService.PatchCustomerAsync(customerId, customer, cancellationToken);

            return updatedCustomer is null ? NotFound() : Ok(updatedCustomer);
        }

        [HttpDelete("{customerId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCustomer(int customerId, CancellationToken cancellationToken)
        {
            var deleted = await _customerService.DeleteCustomerAsync(customerId, cancellationToken);

            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("{customerId:int}/orders")]
        [ProducesResponseType<IReadOnlyCollection<CustomerOrderDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyCollection<CustomerOrderDto>>> GetCustomerOrders(int customerId, CancellationToken cancellationToken)
        {
            var orders = await _customerService.GetCustomerOrdersAsync(customerId, cancellationToken);

            return orders is null ? NotFound() : Ok(orders);
        }

        [HttpGet("{customerId:int}/orders/{orderId:int}")]
        [ProducesResponseType<CustomerOrderDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerOrderDto>> GetCustomerOrderById(
            int customerId,
            int orderId,
            CancellationToken cancellationToken)
        {
            var order = await _customerService.GetCustomerOrderByIdAsync(customerId, orderId, cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }

        [HttpGet("{customerId:int}/summary")]
        [ProducesResponseType<CustomerSummaryDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerSummaryDto>> GetCustomerSummary(int customerId, CancellationToken cancellationToken)
        {
            var summary = await _customerService.GetCustomerSummaryAsync(customerId, cancellationToken);

            return summary is null ? NotFound() : Ok(summary);
        }

        [HttpGet("{customerId:int}/recent-orders")]
        [ProducesResponseType<IReadOnlyCollection<CustomerOrderDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyCollection<CustomerOrderDto>>> GetCustomerRecentOrders(int customerId, CancellationToken cancellationToken)
        {
            var orders = await _customerService.GetCustomerRecentOrdersAsync(customerId, cancellationToken);

            return orders is null ? NotFound() : Ok(orders);
        }

        [HttpGet("{customerId:int}/order-summary")]
        [ProducesResponseType<CustomerOrderSummaryDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerOrderSummaryDto>> GetCustomerOrderSummary(int customerId, CancellationToken cancellationToken)
        {
            var summary = await _customerService.GetCustomerOrderSummaryAsync(customerId, cancellationToken);

            return summary is null ? NotFound() : Ok(summary);
        }

        [HttpGet("{customerId:int}/rewards")]
        [ProducesResponseType<CustomerRewardsDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerRewardsDto>> GetCustomerRewards(int customerId, CancellationToken cancellationToken)
        {
            var rewards = await _customerService.GetCustomerRewardsAsync(customerId, cancellationToken);

            return rewards is null ? NotFound() : Ok(rewards);
        }

        /// <summary>
        /// Lists a customer's addresses.
        /// </summary>
        /// <remarks>
        /// A customer with no addresses is a 200 with an empty array, not a 404 — that's the
        /// majority case (440 of 847 customers have no address). Only an unknown customer is a
        /// 404, which is why the service returns null rather than an empty collection for it.
        /// </remarks>
        [HttpGet("{customerId:int}/addresses")]
        [ProducesResponseType<IReadOnlyCollection<CustomerAddressDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyCollection<CustomerAddressDto>>> GetCustomerAddresses(int customerId, CancellationToken cancellationToken)
        {
            var addresses = await _addressService.GetCustomerAddressesAsync(customerId, cancellationToken);

            return addresses is null ? NotFound() : Ok(addresses);
        }

        [HttpGet("{customerId:int}/addresses/{addressId:int}")]
        [ProducesResponseType<CustomerAddressDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerAddressDto>> GetCustomerAddressById(
            int customerId,
            int addressId,
            CancellationToken cancellationToken)
        {
            var address = await _addressService.GetCustomerAddressByIdAsync(customerId, addressId, cancellationToken);

            return address is null ? NotFound() : Ok(address);
        }
    }
}
