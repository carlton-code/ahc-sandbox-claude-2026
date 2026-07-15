using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Services;
using AHC.Sandbox.Domain.Entities;
using AHC.Sandbox.UnitTests.Addresses.Fakes;
using AHC.Sandbox.UnitTests.Customers.Fakes;

namespace AHC.Sandbox.UnitTests.Addresses
{
    public class AddressServiceTests
    {
        private const int KnownCustomerId = 29503;
        private const int UnknownCustomerId = 999999;

        private FakeAddressReadRepository _addressReadRepository = null!;
        private FakeCustomerReadRepository _customerReadRepository = null!;
        private AddressService _service = null!;

        [SetUp]
        public void Setup()
        {
            _addressReadRepository = new FakeAddressReadRepository();
            _customerReadRepository = new FakeCustomerReadRepository();
            _service = new AddressService(_addressReadRepository, _customerReadRepository);
        }

        private void GivenCustomerExists(int customerId)
        {
            _customerReadRepository.Customers.Add(new Customer
            {
                CustomerId = customerId,
                FirstName = "Jon",
                LastName = "Yang",
                EmailAddress = "jon.yang@example.com"
            });
        }

        private static CustomerAddressDto CreateAddress(int addressId, string addressType = "Main Office")
        {
            return new CustomerAddressDto
            {
                AddressId = addressId,
                AddressLine1 = "25981 College Street",
                City = "Montreal",
                StateProvince = "Quebec",
                CountryRegion = "Canada",
                PostalCode = "H1Y 2H5",
                SingleLineAddress = "25981 College Street, Montreal, Quebec, H1Y 2H5, Canada",
                AddressType = addressType
            };
        }

        // --- GetCustomerAddressesAsync ---------------------------------------------------------

        [Test]
        public async Task GetCustomerAddressesAsync_ReturnsNull_WhenCustomerDoesNotExist()
        {
            var addresses = await _service.GetCustomerAddressesAsync(UnknownCustomerId);

            Assert.That(addresses, Is.Null);
        }

        // The short-circuit is the point: without it the address query would run for a customer
        // that doesn't exist and return empty, which the controller would render as 200 [] rather
        // than 404.
        [Test]
        public async Task GetCustomerAddressesAsync_DoesNotQueryAddresses_WhenCustomerDoesNotExist()
        {
            await _service.GetCustomerAddressesAsync(UnknownCustomerId);

            Assert.That(_addressReadRepository.GetByCustomerIdAsyncCalled, Is.False);
        }

        // The distinction that matters: 440 of 847 customers have no address, so "exists but has
        // none" must stay an empty collection and never collapse into the null that means 404.
        [Test]
        public async Task GetCustomerAddressesAsync_ReturnsEmpty_NotNull_WhenCustomerExistsWithNoAddresses()
        {
            GivenCustomerExists(KnownCustomerId);

            var addresses = await _service.GetCustomerAddressesAsync(KnownCustomerId);

            Assert.That(addresses, Is.Not.Null);
            Assert.That(addresses, Is.Empty);
        }

        [Test]
        public async Task GetCustomerAddressesAsync_ReturnsAddresses_WhenCustomerExists()
        {
            GivenCustomerExists(KnownCustomerId);
            _addressReadRepository.Addresses.Add(CreateAddress(541, "Main Office"));
            _addressReadRepository.Addresses.Add(CreateAddress(32, "Shipping"));

            var addresses = await _service.GetCustomerAddressesAsync(KnownCustomerId);

            Assert.That(addresses, Is.Not.Null);
            Assert.That(addresses!.Select(a => a.AddressId), Is.EqualTo(new[] { 541, 32 }));
            Assert.That(addresses.Select(a => a.AddressType), Is.EqualTo(new[] { "Main Office", "Shipping" }));
        }

        // --- GetCustomerAddressByIdAsync -------------------------------------------------------

        [Test]
        public async Task GetCustomerAddressByIdAsync_ReturnsAddress_WhenItExists()
        {
            _addressReadRepository.Addresses.Add(CreateAddress(541));

            var address = await _service.GetCustomerAddressByIdAsync(KnownCustomerId, 541);

            Assert.That(address, Is.Not.Null);
            Assert.That(address!.AddressId, Is.EqualTo(541));
        }

        // Only the unknown-addressId miss can be tested here: FakeAddressReadRepository ignores
        // customerId, so cross-customer scoping (a real addressId linked to a different customer)
        // lives in the SQL and is covered by AddressReadRepositoryTests in the integration suite.
        [Test]
        public async Task GetCustomerAddressByIdAsync_ReturnsNull_WhenAddressDoesNotExist()
        {
            _addressReadRepository.Addresses.Add(CreateAddress(541));

            var address = await _service.GetCustomerAddressByIdAsync(KnownCustomerId, 999999);

            Assert.That(address, Is.Null);
        }

        // Deliberately no customer-existence probe on this path — a miss is a 404 either way, so
        // the extra query would buy nothing. This pins that decision.
        [Test]
        public async Task GetCustomerAddressByIdAsync_DoesNotProbeCustomer()
        {
            _addressReadRepository.Addresses.Add(CreateAddress(541));

            await _service.GetCustomerAddressByIdAsync(KnownCustomerId, 541);

            Assert.That(_customerReadRepository.GetByIdAsyncCallCount, Is.Zero);
        }
    }
}
