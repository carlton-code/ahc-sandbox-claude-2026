using AHC.Sandbox.Application.Addresses.Dtos;

namespace AHC.Sandbox.Application.Addresses.Interfaces
{
    // Note this returns DTOs rather than Domain entities, unlike ICustomerWriteRepository (whose
    // CreateAsync returns Domain.Customer for CustomerService to map). That's deliberate and local
    // to this slice: IAddressReadRepository already returns DTOs, because CustomerAddressDto
    // carries AddressType from the CustomerAddress link row — which isn't part of Domain.Address
    // and has nowhere to live on it. Splitting the mapping across layers here would mean returning
    // a (Domain.Address, string addressType) pair purely to reassemble it one layer up. Keep the
    // Customer slice as the convention's reference; don't propagate this exception without the
    // same reason.
    public interface IAddressWriteRepository
    {
        /// <summary>
        /// Creates an address and links it to the customer in one unit of work.
        /// </summary>
        /// <remarks>
        /// <c>SalesLT.Address</c> has no owner column — the customer link and the address type live
        /// on <c>SalesLT.CustomerAddress</c> — so creating an address without also writing that link
        /// row would leave an orphan no read path can reach. The two inserts therefore belong to one
        /// method rather than being composable separately.
        /// </remarks>
        Task<CustomerAddressDto> CreateForCustomerAsync(int customerId, CreateCustomerAddressDto address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces an address's fields. Returns <c>false</c> if no such address exists.
        /// </summary>
        Task<bool> UpdateAsync(int addressId, UpdateAddressDto address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an address. Returns <c>false</c> if no such address exists.
        /// </summary>
        /// <remarks>
        /// Throws <c>DbUpdateException</c> when anything still references the address — a customer
        /// link, or an order's ship-to/bill-to. That's translated to a <c>409</c> at the API edge;
        /// see the note on the delete path in <c>AddressWriteRepository</c>.
        /// </remarks>
        Task<bool> DeleteAsync(int addressId, CancellationToken cancellationToken = default);
    }
}
