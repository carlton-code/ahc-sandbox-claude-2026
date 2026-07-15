using AHC.Sandbox.Application.Addresses.Dtos;

namespace AHC.Sandbox.Application.Addresses.Interfaces
{
    public interface IAddressService
    {
        /// <summary>
        /// Returns a customer's addresses, or <c>null</c> if the customer doesn't exist.
        /// </summary>
        /// <remarks>
        /// An empty collection and <c>null</c> mean different things: empty is a customer who
        /// exists but has no address (440 of 847 customers today), null is a customer who doesn't
        /// exist at all. Only the latter is a 404.
        /// </remarks>
        Task<IReadOnlyCollection<CustomerAddressDto>?> GetCustomerAddressesAsync(int customerId, CancellationToken cancellationToken = default);

        Task<CustomerAddressDto?> GetCustomerAddressByIdAsync(int customerId, int addressId, CancellationToken cancellationToken = default);
    }
}
