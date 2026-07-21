using AHC.Sandbox.Application.Addresses.Dtos;

namespace AHC.Sandbox.Application.Addresses.Interfaces
{
    // Reads only — writes live on IAddressWriteRepository, per this layer's split read/write
    // convention.
    public interface IAddressReadRepository
    {
        Task<IReadOnlyCollection<CustomerAddressDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerAddressDto?> GetByCustomerAndAddressIdAsync(int customerId, int addressId, CancellationToken cancellationToken = default);

        // No customer in the signature: this backs GET /api/v1/addresses/{addressId}, which reads
        // the address record itself and so returns AddressDto rather than CustomerAddressDto.
        Task<AddressDto?> GetByIdAsync(int addressId, CancellationToken cancellationToken = default);
    }
}
