using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Products.Fakes
{
    // Minimal hand-written in-memory fake for IProductReadRepository, following
    // FakeCustomerReadRepository's pattern. Exposes a call counter so tests can assert whether
    // the repository was reached at all.
    public class FakeProductReadRepository : IProductReadRepository
    {
        public List<Product> Products { get; } = new();

        public int GetByIdAsyncCallCount { get; private set; }

        public Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Product>>(Products);

        public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            GetByIdAsyncCallCount++;
            return Task.FromResult(Products.FirstOrDefault(p => p.ProductId == productId));
        }
    }
}
