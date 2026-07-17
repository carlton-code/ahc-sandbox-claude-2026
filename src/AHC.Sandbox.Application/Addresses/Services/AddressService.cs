using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Interfaces;
using AHC.Sandbox.Application.Customers.Interfaces;

namespace AHC.Sandbox.Application.Addresses.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressReadRepository _addressReadRepository;
        private readonly ICustomerReadRepository _customerReadRepository;

        public AddressService(
            IAddressReadRepository addressReadRepository,
            ICustomerReadRepository customerReadRepository)
        {
            _addressReadRepository = addressReadRepository;
            _customerReadRepository = customerReadRepository;
        }

        public async Task<IReadOnlyCollection<CustomerAddressDto>?> GetCustomerAddressesAsync(
            int customerId,
            CancellationToken cancellationToken = default)
        {
            // The address query alone can't tell "no such customer" from "customer has no
            // addresses" — both return zero rows — so the customer is probed first.
            var customer = await _customerReadRepository.GetByIdAsync(customerId, cancellationToken);

            if (customer is null)
            {
                return null;
            }

            return await _addressReadRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        }

        // No customer probe here: a miss is a 404 either way, so distinguishing "customer doesn't
        // exist" from "customer doesn't have this address" would cost a query and change nothing.
        public Task<CustomerAddressDto?> GetCustomerAddressByIdAsync(
            int customerId,
            int addressId,
            CancellationToken cancellationToken = default)
        {
            return _addressReadRepository.GetByCustomerAndAddressIdAsync(customerId, addressId, cancellationToken);
        }
    }
}
