using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Interfaces;

namespace AHC.Sandbox.UnitTests.Addresses.Fakes
{
    // Minimal hand-written in-memory fake for IAddressReadRepository, mirroring
    // FakeCustomerReadRepository. Exposes a call flag so tests can prove the not-found
    // short-circuit in AddressService stopped before reaching the address query.
    public class FakeAddressReadRepository : IAddressReadRepository
    {
        public List<CustomerAddressDto> Addresses { get; } = new();

        public bool GetByCustomerIdAsyncCalled { get; private set; }

        public Task<IReadOnlyCollection<CustomerAddressDto>> GetByCustomerIdAsync(
            int customerId,
            CancellationToken cancellationToken = default)
        {
            GetByCustomerIdAsyncCalled = true;

            return Task.FromResult<IReadOnlyCollection<CustomerAddressDto>>(Addresses);
        }

        public Task<CustomerAddressDto?> GetByCustomerAndAddressIdAsync(
            int customerId,
            int addressId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Addresses.FirstOrDefault(a => a.AddressId == addressId));
        }
    }
}
