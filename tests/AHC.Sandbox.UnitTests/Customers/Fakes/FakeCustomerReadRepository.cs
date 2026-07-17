using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Customers.Fakes
{
    // Minimal hand-written in-memory fake for ICustomerReadRepository. Exposes call counters so
    // tests can assert whether a given method was reached at all (e.g. to prove a not-found
    // short-circuit prevented a follow-up repository call).
    public class FakeCustomerReadRepository : ICustomerReadRepository
    {
        public List<Customer> Customers { get; } = new();

        public IReadOnlyCollection<Customer> SearchResultsToReturn { get; set; } = Array.Empty<Customer>();

        public int GetByIdAsyncCallCount { get; private set; }
        public string? LastSearchTerm { get; private set; }

        public Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Customer>>(Customers);

        public Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            GetByIdAsyncCallCount++;
            return Task.FromResult(Customers.FirstOrDefault(c => c.CustomerId == customerId));
        }

        // Records the term rather than matching on it: the real matching is SQL, so it's proven in
        // the integration tests, not faked here.
        public Task<IReadOnlyCollection<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            LastSearchTerm = searchTerm;
            return Task.FromResult(SearchResultsToReturn);
        }

        public Task<CustomerSummaryDto?> GetSummaryAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<CustomerSummaryDto?>(null);

        public Task<CustomerOrderSummaryDto?> GetOrderSummaryAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<CustomerOrderSummaryDto?>(null);

        public Task<CustomerRewardsDto?> GetRewardsAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<CustomerRewardsDto?>(null);
    }
}
