using AHC.Sandbox.Application.Addresses.Dtos;

namespace AHC.Sandbox.Application.Addresses.Interfaces
{
    // Read-only by design: nothing in this slice writes addresses, so there's no
    // IAddressWriteRepository counterpart yet. Add one alongside this if writes arrive, rather
    // than widening this interface.
    public interface IAddressReadRepository
    {
        Task<IReadOnlyCollection<CustomerAddressDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerAddressDto?> GetByCustomerAndAddressIdAsync(int customerId, int addressId, CancellationToken cancellationToken = default);
    }
}
