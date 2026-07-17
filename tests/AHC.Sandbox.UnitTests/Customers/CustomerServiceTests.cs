using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Services;
using AHC.Sandbox.Domain.Entities;
using AHC.Sandbox.UnitTests.Customers.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AHC.Sandbox.UnitTests.Customers
{
    public class CustomerServiceTests
    {
        private FakeCustomerReadRepository _readRepository = null!;
        private FakeCustomerWriteRepository _writeRepository = null!;
        private FakeCustomerCacheRepository _cacheRepository = null!;
        private CustomerService _service = null!;

        [SetUp]
        public void Setup()
        {
            _readRepository = new FakeCustomerReadRepository();
            _writeRepository = new FakeCustomerWriteRepository();
            _cacheRepository = new FakeCustomerCacheRepository();
            _service = new CustomerService(
                _readRepository,
                _writeRepository,
                _cacheRepository,
                NullLogger<CustomerService>.Instance);
        }

        private static Customer CreateCustomer(
            int customerId,
            string firstName = "Jon",
            string? middleName = null,
            string lastName = "Yang",
            string? companyName = "Adventure Works",
            string emailAddress = "jon.yang@example.com")
        {
            return new Customer
            {
                CustomerId = customerId,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                CompanyName = companyName,
                EmailAddress = emailAddress
            };
        }

        // --- GetCustomersAsync ---------------------------------------------------------------

        [Test]
        public async Task GetCustomersAsync_MapsAllRepositoryCustomersToDtos()
        {
            _readRepository.Customers.Add(CreateCustomer(1, firstName: "Jon", lastName: "Yang"));
            _readRepository.Customers.Add(CreateCustomer(2, firstName: "Eugene", middleName: "L", lastName: "Huang"));

            var result = await _service.GetCustomersAsync();

            Assert.That(result, Has.Count.EqualTo(2));

            var first = result.Single(c => c.CustomerId == 1);
            Assert.That(first.FirstName, Is.EqualTo("Jon"));
            Assert.That(first.LastName, Is.EqualTo("Yang"));
            Assert.That(first.FullName, Is.EqualTo("Jon Yang"));

            var second = result.Single(c => c.CustomerId == 2);
            Assert.That(second.FirstName, Is.EqualTo("Eugene"));
            Assert.That(second.MiddleName, Is.EqualTo("L"));
            Assert.That(second.LastName, Is.EqualTo("Huang"));
            Assert.That(second.FullName, Is.EqualTo("Eugene L Huang"));
        }

        // --- SearchCustomersAsync -------------------------------------------------------------

        [Test]
        public async Task SearchCustomersAsync_MapsRepositoryResultsToDtos()
        {
            _readRepository.SearchResultsToReturn =
            [
                CreateCustomer(1, firstName: "Orlando", lastName: "Gee"),
                CreateCustomer(2, firstName: "Roger", lastName: "Van Houten")
            ];

            var result = await _service.SearchCustomersAsync("o");

            Assert.That(result, Has.Count.EqualTo(2));

            var first = result.Single(c => c.CustomerId == 1);
            Assert.That(first.FullName, Is.EqualTo("Orlando Gee"));

            var second = result.Single(c => c.CustomerId == 2);
            Assert.That(second.FullName, Is.EqualTo("Roger Van Houten"));
        }

        [Test]
        public async Task SearchCustomersAsync_PassesTermToRepositoryUnmodified()
        {
            await _service.SearchCustomersAsync("Orlando Gee");

            Assert.That(_readRepository.LastSearchTerm, Is.EqualTo("Orlando Gee"));
        }

        // The fake ignores the term, so this pins only "repository returns empty => service
        // returns empty, not null". Whether a given term actually matches is the SQL's job and is
        // covered in CustomerReadRepositoryTests.
        [Test]
        public async Task SearchCustomersAsync_RepositoryReturnsNothing_ServiceReturnsEmptyNotNull()
        {
            var result = await _service.SearchCustomersAsync("anything");

            Assert.That(result, Is.Empty);
        }

        // --- GetCustomerByIdAsync -------------------------------------------------------------

        [Test]
        public async Task GetCustomerByIdAsync_CacheHit_ReturnsCachedDtoWithoutHittingReadRepository()
        {
            var cached = new CustomerDto
            {
                CustomerId = 1,
                FirstName = "Jon",
                LastName = "Yang",
                FullName = "Jon Yang",
                EmailAddress = "jon.yang@example.com"
            };
            _cacheRepository.Seed(1, cached);

            var result = await _service.GetCustomerByIdAsync(1);

            Assert.That(result, Is.SameAs(cached));
            Assert.That(_readRepository.GetByIdAsyncCallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task GetCustomerByIdAsync_CacheMiss_FallsThroughToReadRepositoryAndPopulatesCache()
        {
            _readRepository.Customers.Add(CreateCustomer(1, firstName: "Jon", lastName: "Yang"));

            var result = await _service.GetCustomerByIdAsync(1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.CustomerId, Is.EqualTo(1));
            Assert.That(result.FullName, Is.EqualTo("Jon Yang"));
            Assert.That(_readRepository.GetByIdAsyncCallCount, Is.EqualTo(1));

            Assert.That(_cacheRepository.SetAsyncCalled, Is.True);
            Assert.That(_cacheRepository.Contains(1), Is.True);
        }

        [Test]
        public async Task GetCustomerByIdAsync_CustomerDoesNotExist_ReturnsNull()
        {
            var result = await _service.GetCustomerByIdAsync(999);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetCustomerByIdAsync_CacheUnavailableOnRead_FallsBackToReadRepositoryWithoutCaching()
        {
            _readRepository.Customers.Add(CreateCustomer(1, firstName: "Jon", lastName: "Yang"));
            _cacheRepository.ThrowOnGet = true;

            var result = await _service.GetCustomerByIdAsync(1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.CustomerId, Is.EqualTo(1));
            Assert.That(_readRepository.GetByIdAsyncCallCount, Is.EqualTo(1));

            // The cache read already proved Redis is unreachable, so the service shouldn't
            // attempt a follow-up write to the same unreachable cache.
            Assert.That(_cacheRepository.SetAsyncCalled, Is.False);
        }

        // --- CreateCustomerAsync ---------------------------------------------------------------

        [Test]
        public async Task CreateCustomerAsync_DelegatesToWriteRepositoryAndMapsResult()
        {
            var createDto = new CreateCustomerDto
            {
                FirstName = "Jon",
                LastName = "Yang",
                EmailAddress = "jon.yang@example.com"
            };
            _writeRepository.CustomerToReturnOnCreate = CreateCustomer(42, firstName: "Jon", lastName: "Yang");

            var result = await _service.CreateCustomerAsync(createDto);

            Assert.That(_writeRepository.LastCreateRequest, Is.SameAs(createDto));
            Assert.That(result.CustomerId, Is.EqualTo(42));
            Assert.That(result.FullName, Is.EqualTo("Jon Yang"));
        }

        // --- UpdateCustomerAsync ---------------------------------------------------------------

        [Test]
        public async Task UpdateCustomerAsync_WriteSucceeds_InvalidatesCache()
        {
            _writeRepository.UpdateResult = true;
            var updateDto = new UpdateCustomerDto { FirstName = "Jon", LastName = "Yang", EmailAddress = "jon.yang@example.com" };

            var result = await _service.UpdateCustomerAsync(1, updateDto);

            Assert.That(result, Is.True);
            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.True);
            Assert.That(_cacheRepository.LastRemovedCustomerId, Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateCustomerAsync_WriteFails_DoesNotTouchCache()
        {
            _writeRepository.UpdateResult = false;
            var updateDto = new UpdateCustomerDto { FirstName = "Jon", LastName = "Yang", EmailAddress = "jon.yang@example.com" };

            var result = await _service.UpdateCustomerAsync(1, updateDto);

            Assert.That(result, Is.False);
            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.False);
        }

        // --- PatchCustomerAsync ----------------------------------------------------------------

        [Test]
        public async Task PatchCustomerAsync_MergesOnlyNonNullFieldsOntoExistingCustomer()
        {
            _readRepository.Customers.Add(CreateCustomer(
                1,
                firstName: "Jon",
                middleName: "V",
                lastName: "Yang",
                companyName: "Adventure Works",
                emailAddress: "jon.yang@example.com"));

            var patch = new PatchCustomerDto
            {
                FirstName = "Jonathan",
                EmailAddress = "jonathan.yang@example.com"
                // MiddleName, LastName, CompanyName intentionally left null.
            };

            await _service.PatchCustomerAsync(1, patch);

            Assert.That(_writeRepository.LastUpdateRequest, Is.Not.Null);
            var (customerId, sentUpdate) = _writeRepository.LastUpdateRequest!.Value;
            Assert.That(customerId, Is.EqualTo(1));
            Assert.That(sentUpdate.FirstName, Is.EqualTo("Jonathan"));
            Assert.That(sentUpdate.MiddleName, Is.EqualTo("V"));
            Assert.That(sentUpdate.LastName, Is.EqualTo("Yang"));
            Assert.That(sentUpdate.CompanyName, Is.EqualTo("Adventure Works"));
            Assert.That(sentUpdate.EmailAddress, Is.EqualTo("jonathan.yang@example.com"));
        }

        [Test]
        public async Task PatchCustomerAsync_WriteSucceeds_InvalidatesCache()
        {
            _readRepository.Customers.Add(CreateCustomer(1));
            _cacheRepository.Seed(1, new CustomerDto { CustomerId = 1, FullName = "Stale Name" });

            await _service.PatchCustomerAsync(1, new PatchCustomerDto { FirstName = "Updated" });

            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.True);
            Assert.That(_cacheRepository.LastRemovedCustomerId, Is.EqualTo(1));
        }

        [Test]
        public async Task PatchCustomerAsync_CustomerDoesNotExist_ReturnsNull()
        {
            var result = await _service.PatchCustomerAsync(999, new PatchCustomerDto { FirstName = "Updated" });

            Assert.That(result, Is.Null);
        }

        // --- DeleteCustomerAsync ---------------------------------------------------------------

        [Test]
        public async Task DeleteCustomerAsync_DeleteSucceeds_InvalidatesCache()
        {
            _writeRepository.DeleteResult = true;

            var result = await _service.DeleteCustomerAsync(1);

            Assert.That(result, Is.True);
            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.True);
            Assert.That(_cacheRepository.LastRemovedCustomerId, Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteCustomerAsync_DeleteFails_DoesNotTouchCache()
        {
            _writeRepository.DeleteResult = false;

            var result = await _service.DeleteCustomerAsync(1);

            Assert.That(result, Is.False);
            Assert.That(_cacheRepository.RemoveAsyncCalled, Is.False);
        }

    }
}
