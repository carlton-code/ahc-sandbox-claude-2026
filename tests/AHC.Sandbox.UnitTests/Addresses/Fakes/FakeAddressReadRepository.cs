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

        // Projects the same backing list down to AddressDto, dropping AddressType — the shape the
        // real repository returns for the customer-less GET /api/v1/addresses/{addressId}.
        public Task<AddressDto?> GetByIdAsync(int addressId, CancellationToken cancellationToken = default)
        {
            var match = Addresses.FirstOrDefault(a => a.AddressId == addressId);

            if (match is null)
            {
                return Task.FromResult<AddressDto?>(null);
            }

            return Task.FromResult<AddressDto?>(new AddressDto
            {
                AddressId = match.AddressId,
                AddressLine1 = match.AddressLine1,
                AddressLine2 = match.AddressLine2,
                City = match.City,
                StateProvince = match.StateProvince,
                CountryRegion = match.CountryRegion,
                PostalCode = match.PostalCode,
                SingleLineAddress = match.SingleLineAddress
            });
        }
    }
}
