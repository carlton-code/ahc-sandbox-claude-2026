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

        /// <summary>
        /// Returns the address record itself, or <c>null</c> if there's no such address.
        /// </summary>
        Task<AddressDto?> GetAddressByIdAsync(int addressId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an address for a customer, or returns <c>null</c> if the customer doesn't exist.
        /// </summary>
        /// <remarks>
        /// Same null-means-404 discipline as <see cref="GetCustomerAddressesAsync"/>: the customer
        /// is probed first, because the insert would otherwise fail on the link row's foreign key
        /// and surface as a 409 for what is really a "no such customer" 404.
        /// </remarks>
        Task<CustomerAddressDto?> CreateCustomerAddressAsync(int customerId, CreateCustomerAddressDto address, CancellationToken cancellationToken = default);

        Task<bool> UpdateAddressAsync(int addressId, UpdateAddressDto address, CancellationToken cancellationToken = default);

        Task<bool> DeleteAddressAsync(int addressId, CancellationToken cancellationToken = default);
    }
}
