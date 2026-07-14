using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;

namespace AHC.Sandbox.UnitTests.Customers.Fakes
{
    // Minimal hand-written in-memory fake for ICustomerCacheRepository. Each Throw* flag lets a
    // test simulate the cache backend being unreachable (CacheUnavailableException) on that
    // specific operation, mirroring how the real Redis-backed implementation can fail.
    public class FakeCustomerCacheRepository : ICustomerCacheRepository
    {
        private readonly Dictionary<int, CustomerDto> _cache = new();

        public bool ThrowOnGet { get; set; }
        public bool ThrowOnSet { get; set; }
        public bool ThrowOnRemove { get; set; }

        public bool GetByIdAsyncCalled { get; private set; }
        public bool SetAsyncCalled { get; private set; }
        public bool RemoveAsyncCalled { get; private set; }
        public int? LastRemovedCustomerId { get; private set; }

        public void Seed(int customerId, CustomerDto customer) => _cache[customerId] = customer;

        public bool Contains(int customerId) => _cache.ContainsKey(customerId);

        public Task<CustomerDto?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            GetByIdAsyncCalled = true;

            if (ThrowOnGet)
            {
                throw new CacheUnavailableException("Simulated cache outage.", new InvalidOperationException());
            }

            _cache.TryGetValue(customerId, out var customer);
            return Task.FromResult(customer);
        }

        public Task SetAsync(int customerId, CustomerDto customer, CancellationToken cancellationToken = default)
        {
            SetAsyncCalled = true;

            if (ThrowOnSet)
            {
                throw new CacheUnavailableException("Simulated cache outage.", new InvalidOperationException());
            }

            _cache[customerId] = customer;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(int customerId, CancellationToken cancellationToken = default)
        {
            RemoveAsyncCalled = true;
            LastRemovedCustomerId = customerId;

            if (ThrowOnRemove)
            {
                throw new CacheUnavailableException("Simulated cache outage.", new InvalidOperationException());
            }

            _cache.Remove(customerId);
            return Task.CompletedTask;
        }
    }
}
