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
        private FakeAddressWriteRepository _addressWriteRepository = null!;
        private FakeCustomerReadRepository _customerReadRepository = null!;
        private AddressService _service = null!;

        [SetUp]
        public void Setup()
        {
            _addressReadRepository = new FakeAddressReadRepository();
            _addressWriteRepository = new FakeAddressWriteRepository();
            _customerReadRepository = new FakeCustomerReadRepository();
            _service = new AddressService(_addressReadRepository, _addressWriteRepository, _customerReadRepository);
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

        // --- GetAddressByIdAsync ---------------------------------------------------------------

        [Test]
        public async Task GetAddressByIdAsync_ReturnsAddressWithoutType_WhenItExists()
        {
            _addressReadRepository.Addresses.Add(CreateAddress(541));

            var address = await _service.GetAddressByIdAsync(541);

            Assert.That(address, Is.Not.Null);
            Assert.That(address!.AddressId, Is.EqualTo(541));
            Assert.That(
                address.SingleLineAddress,
                Is.EqualTo("25981 College Street, Montreal, Quebec, H1Y 2H5, Canada"));
        }

        [Test]
        public async Task GetAddressByIdAsync_ReturnsNull_WhenAddressDoesNotExist()
        {
            var address = await _service.GetAddressByIdAsync(999999);

            Assert.That(address, Is.Null);
        }

        // --- CreateCustomerAddressAsync ---------------------------------------------------------

        private static CreateCustomerAddressDto CreateAddressRequest(string addressType = "Shipping")
        {
            return new CreateCustomerAddressDto
            {
                AddressLine1 = "1 Test Street",
                City = "Seattle",
                StateProvince = "Washington",
                CountryRegion = "United States",
                PostalCode = "98104",
                AddressType = addressType
            };
        }

        [Test]
        public async Task CreateCustomerAddressAsync_ReturnsNull_WhenCustomerDoesNotExist()
        {
            var created = await _service.CreateCustomerAddressAsync(UnknownCustomerId, CreateAddressRequest());

            Assert.That(created, Is.Null);
        }

        // The probe earns its query here: without it the CustomerAddress insert fails on its
        // foreign key, and the API answers a bad customer id with 409 instead of the 404 it is.
        [Test]
        public async Task CreateCustomerAddressAsync_DoesNotWrite_WhenCustomerDoesNotExist()
        {
            await _service.CreateCustomerAddressAsync(UnknownCustomerId, CreateAddressRequest());

            Assert.That(_addressWriteRepository.CreateForCustomerAsyncCallCount, Is.Zero);
        }

        [Test]
        public async Task CreateCustomerAddressAsync_ReturnsCreatedAddress_WhenCustomerExists()
        {
            GivenCustomerExists(KnownCustomerId);
            _addressWriteRepository.NextAddressId = 11001;

            var created = await _service.CreateCustomerAddressAsync(KnownCustomerId, CreateAddressRequest());

            Assert.That(created, Is.Not.Null);
            Assert.That(created!.AddressId, Is.EqualTo(11001));
            Assert.That(created.AddressType, Is.EqualTo("Shipping"));
        }

        [Test]
        public async Task CreateCustomerAddressAsync_PassesCustomerIdAndBodyToWriteRepository()
        {
            GivenCustomerExists(KnownCustomerId);
            var request = CreateAddressRequest();

            await _service.CreateCustomerAddressAsync(KnownCustomerId, request);

            Assert.That(_addressWriteRepository.LastCreatedForCustomerId, Is.EqualTo(KnownCustomerId));
            Assert.That(_addressWriteRepository.LastCreated, Is.SameAs(request));
        }

        // --- UpdateAddressAsync / DeleteAddressAsync --------------------------------------------

        private static UpdateAddressDto UpdateAddressRequest()
        {
            return new UpdateAddressDto
            {
                AddressLine1 = "2 Test Street",
                City = "Seattle",
                StateProvince = "Washington",
                CountryRegion = "United States",
                PostalCode = "98104"
            };
        }

        [Test]
        public async Task UpdateAddressAsync_PassesRepositoryResultThrough([Values(true, false)] bool repositoryResult)
        {
            _addressWriteRepository.UpdateResult = repositoryResult;

            var updated = await _service.UpdateAddressAsync(541, UpdateAddressRequest());

            Assert.That(updated, Is.EqualTo(repositoryResult));
        }

        // Without this the service could forward a wrong or hardcoded id and every other test here
        // would still pass — only the bool result is pinned elsewhere.
        [Test]
        public async Task UpdateAddressAsync_ForwardsAddressIdAndBody()
        {
            var request = UpdateAddressRequest();

            await _service.UpdateAddressAsync(541, request);

            Assert.That(_addressWriteRepository.LastUpdatedAddressId, Is.EqualTo(541));
            Assert.That(_addressWriteRepository.LastUpdated, Is.SameAs(request));
        }

        [Test]
        public async Task DeleteAddressAsync_ForwardsAddressId()
        {
            await _service.DeleteAddressAsync(541);

            Assert.That(_addressWriteRepository.LastDeletedAddressId, Is.EqualTo(541));
        }

        // No customer probe on update or delete: neither route names a customer, and the
        // repository's own miss is already the 404.
        [Test]
        public async Task UpdateAddressAsync_DoesNotProbeCustomer()
        {
            await _service.UpdateAddressAsync(541, UpdateAddressRequest());

            Assert.That(_customerReadRepository.GetByIdAsyncCallCount, Is.Zero);
        }

        [Test]
        public async Task DeleteAddressAsync_DoesNotProbeCustomer()
        {
            await _service.DeleteAddressAsync(541);

            Assert.That(_customerReadRepository.GetByIdAsyncCallCount, Is.Zero);
        }

        [Test]
        public async Task DeleteAddressAsync_PassesRepositoryResultThrough([Values(true, false)] bool repositoryResult)
        {
            _addressWriteRepository.DeleteResult = repositoryResult;

            var deleted = await _service.DeleteAddressAsync(541);

            Assert.That(deleted, Is.EqualTo(repositoryResult));
        }
    }
}
