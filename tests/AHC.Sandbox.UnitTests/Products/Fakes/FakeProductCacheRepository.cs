using AHC.Sandbox.Application.Caching;
using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;

namespace AHC.Sandbox.UnitTests.Products.Fakes
{
    // Minimal hand-written in-memory fake for IProductCacheRepository. Each Throw* flag lets a
    // test simulate the cache backend being unreachable (CacheUnavailableException) on that
    // specific operation, mirroring how the real Redis-backed implementation can fail.
    public class FakeProductCacheRepository : IProductCacheRepository
    {
        private readonly Dictionary<int, ProductDto> _cache = new();

        public bool ThrowOnGet { get; set; }
        public bool ThrowOnSet { get; set; }
        public bool ThrowOnRemove { get; set; }

        public bool GetByIdAsyncCalled { get; private set; }
        public bool SetAsyncCalled { get; private set; }
        public bool RemoveAsyncCalled { get; private set; }
        public int? LastRemovedProductId { get; private set; }
        public List<int> RemovedProductIds { get; } = new();

        public void Seed(int productId, ProductDto product) => _cache[productId] = product;

        public bool Contains(int productId) => _cache.ContainsKey(productId);

        public Task<ProductDto?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            GetByIdAsyncCalled = true;

            if (ThrowOnGet)
            {
                throw new CacheUnavailableException("Simulated cache outage.", new InvalidOperationException());
            }

            _cache.TryGetValue(productId, out var product);
            return Task.FromResult(product);
        }

        public Task SetAsync(int productId, ProductDto product, CancellationToken cancellationToken = default)
        {
            SetAsyncCalled = true;

            if (ThrowOnSet)
            {
                throw new CacheUnavailableException("Simulated cache outage.", new InvalidOperationException());
            }

            _cache[productId] = product;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(int productId, CancellationToken cancellationToken = default)
        {
            RemoveAsyncCalled = true;
            LastRemovedProductId = productId;
            RemovedProductIds.Add(productId);

            if (ThrowOnRemove)
            {
                throw new CacheUnavailableException("Simulated cache outage.", new InvalidOperationException());
            }

            _cache.Remove(productId);
            return Task.CompletedTask;
        }
    }
}
