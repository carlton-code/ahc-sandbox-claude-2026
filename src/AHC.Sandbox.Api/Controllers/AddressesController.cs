using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AHC.Sandbox.Api.Controllers
{
    /// <summary>
    /// <para>Controller for creating, reading, updating and deleting addresses.</para>
    /// <para>
    /// Deliberately spans two route prefixes, which is why it carries no class-level
    /// <c>[Route]</c> and each action declares an absolute template instead. <c>SalesLT.Address</c>
    /// has no owner column — the customer link and <c>AddressType</c> live on
    /// <c>SalesLT.CustomerAddress</c> — so <em>creating</em> an address needs a customer in the
    /// route (otherwise it orphans a row nothing can reach), while <em>reading, editing and
    /// deleting</em> one doesn't. Two <c>[Route]</c> attributes wouldn't express this: each action
    /// template combines with every controller route, so both prefixes would be generated for
    /// every action. See <c>docs/adr/0013-address-writes-split-across-two-route-prefixes.md</c>.
    /// </para>
    /// <para>
    /// The two nested address <em>reads</em> stay on <see cref="CustomersController"/>
    /// (<c>GET /api/v1/customers/{customerId}/addresses[/{addressId}]</c>) — they're customer-scoped
    /// list views, and moving them here would change routes that already ship.
    /// </para>
    /// <para>
    /// Like <see cref="CustomersController"/>, actions return <c>ActionResult&lt;T&gt;</c> where
    /// there's a body and declare <em>every</em> status with <c>[ProducesResponseType]</c> —
    /// including the success one, since adding any such attribute replaces the framework's inferred
    /// 200 rather than adding to it.
    /// </para>
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressesController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        /// <summary>
        /// Creates an address and attaches it to the customer.
        /// </summary>
        /// <remarks>
        /// Nested under the customer because the address and its <c>SalesLT.CustomerAddress</c> link
        /// row are written together — there's no way to create an unattached address through this
        /// API, by design. Returns <c>404</c> for an unknown customer. The <c>Location</c> header
        /// points at the top-level <c>GET /api/v1/addresses/{addressId}</c>.
        /// </remarks>
        /// <response code="409">
        /// The customer-existence check is a check-then-act: a customer deleted between the probe
        /// and the insert leaves the link row's foreign key to fail, which
        /// <c>DatabaseConflictExceptionHandler</c> renders as a conflict. Rare, but declared so the
        /// OpenAPI document is honest about it.
        /// </response>
        [HttpPost("api/v1/customers/{customerId:int}/addresses")]
        [ProducesResponseType<CustomerAddressDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CustomerAddressDto>> CreateCustomerAddress(
            int customerId,
            [FromBody] CreateCustomerAddressDto address,
            CancellationToken cancellationToken)
        {
            var createdAddress = await _addressService.CreateCustomerAddressAsync(customerId, address, cancellationToken);

            if (createdAddress is null)
            {
                return NotFound();
            }

            return CreatedAtAction(
                nameof(GetAddressById),
                new { addressId = createdAddress.AddressId },
                createdAddress);
        }

        /// <summary>
        /// Gets an address by id.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="AddressDto"/>, which has no <c>addressType</c> — that describes a
        /// customer↔address link rather than the address, and this route names no customer. Use
        /// <c>GET /api/v1/customers/{customerId}/addresses/{addressId}</c> when the type is needed.
        /// </remarks>
        [HttpGet("api/v1/addresses/{addressId:int}")]
        [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AddressDto>> GetAddressById(int addressId, CancellationToken cancellationToken)
        {
            var address = await _addressService.GetAddressByIdAsync(addressId, cancellationToken);

            return address is null ? NotFound() : Ok(address);
        }

        /// <summary>
        /// Replaces an address.
        /// </summary>
        /// <remarks>
        /// Edits the shared address row, so it affects every customer linked to it — today no
        /// address is linked to more than one customer, but the route level is where that would
        /// show. The customer↔address <c>addressType</c> is not editable here.
        /// </remarks>
        [HttpPut("api/v1/addresses/{addressId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAddress(
            int addressId,
            [FromBody] UpdateAddressDto address,
            CancellationToken cancellationToken)
        {
            var updated = await _addressService.UpdateAddressAsync(addressId, address, cancellationToken);

            return updated ? NoContent() : NotFound();
        }

        /// <summary>
        /// Deletes an address.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns <c>409</c> when anything still references the address — a customer link, or an
        /// order's <c>ShipToAddressID</c>/<c>BillToAddressID</c>. Every foreign key in this database
        /// is <c>NO_ACTION</c>, and this endpoint deliberately does not unlink first, matching
        /// <c>docs/adr/0009-customer-delete-refuses-rather-than-cascades.md</c>.
        /// </para>
        /// <para>
        /// In practice that makes <c>409</c> the <em>only</em> outcome for an address this API can
        /// reach: creating one always writes a customer link row, and every seeded address has one
        /// too, so nothing reachable here is unreferenced. <c>204</c> is declared because the
        /// repository can return it, not because a caller can currently provoke it. That's the
        /// intended trade-off, not a gap — see
        /// <c>docs/adr/0013-address-writes-split-across-two-route-prefixes.md</c>.
        /// </para>
        /// <para>The <c>409</c> comes from <c>DatabaseConflictExceptionHandler</c>, not this action.</para>
        /// </remarks>
        [HttpDelete("api/v1/addresses/{addressId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteAddress(int addressId, CancellationToken cancellationToken)
        {
            var deleted = await _addressService.DeleteAddressAsync(addressId, cancellationToken);

            return deleted ? NoContent() : NotFound();
        }
    }
}
