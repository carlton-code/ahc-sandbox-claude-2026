using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Interfaces;

namespace AHC.Sandbox.UnitTests.Addresses.Fakes
{
    // Minimal hand-written fake for IAddressWriteRepository. Records what it was asked to do so
    // tests can prove AddressService's customer probe short-circuits before any write is attempted,
    // and lets each result be configured so the true/false pass-through can be pinned both ways.
    public class FakeAddressWriteRepository : IAddressWriteRepository
    {
        public int CreateForCustomerAsyncCallCount { get; private set; }

        public int? LastCreatedForCustomerId { get; private set; }

        public CreateCustomerAddressDto? LastCreated { get; private set; }

        public UpdateAddressDto? LastUpdated { get; private set; }

        public int? LastUpdatedAddressId { get; private set; }

        public int? LastDeletedAddressId { get; private set; }

        public int NextAddressId { get; set; } = 1;

        public bool UpdateResult { get; set; } = true;

        public bool DeleteResult { get; set; } = true;

        public Task<CustomerAddressDto> CreateForCustomerAsync(
            int customerId,
            CreateCustomerAddressDto address,
            CancellationToken cancellationToken = default)
        {
            CreateForCustomerAsyncCallCount++;
            LastCreatedForCustomerId = customerId;
            LastCreated = address;

            return Task.FromResult(new CustomerAddressDto
            {
                AddressId = NextAddressId,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                StateProvince = address.StateProvince,
                CountryRegion = address.CountryRegion,
                PostalCode = address.PostalCode,
                SingleLineAddress = string.Join(
                    ", ",
                    new[]
                    {
                        address.AddressLine1,
                        address.AddressLine2,
                        address.City,
                        address.StateProvince,
                        address.PostalCode,
                        address.CountryRegion
                    }.Where(part => !string.IsNullOrWhiteSpace(part))),
                AddressType = address.AddressType
            });
        }

        public Task<bool> UpdateAsync(
            int addressId,
            UpdateAddressDto address,
            CancellationToken cancellationToken = default)
        {
            LastUpdated = address;
            LastUpdatedAddressId = addressId;

            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteAsync(int addressId, CancellationToken cancellationToken = default)
        {
            LastDeletedAddressId = addressId;

            return Task.FromResult(DeleteResult);
        }
    }
}
