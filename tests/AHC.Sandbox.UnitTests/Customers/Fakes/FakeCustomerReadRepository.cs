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

        public IReadOnlyCollection<CustomerOrderDto> OrdersToReturn { get; set; } = Array.Empty<CustomerOrderDto>();
        public IReadOnlyCollection<CustomerOrderDto> RecentOrdersToReturn { get; set; } = Array.Empty<CustomerOrderDto>();

        public int GetByIdAsyncCallCount { get; private set; }
        public bool GetOrdersByCustomerIdAsyncCalled { get; private set; }
        public bool GetRecentOrdersAsyncCalled { get; private set; }

        public Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Customer>>(Customers);

        public Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            GetByIdAsyncCallCount++;
            return Task.FromResult(Customers.FirstOrDefault(c => c.CustomerId == customerId));
        }

        public Task<IReadOnlyCollection<CustomerOrderDto>> GetOrdersByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            GetOrdersByCustomerIdAsyncCalled = true;
            return Task.FromResult(OrdersToReturn);
        }

        public Task<CustomerOrderDto?> GetOrderByIdAsync(int customerId, int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<CustomerOrderDto?>(null);

        public Task<CustomerSummaryDto?> GetSummaryAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<CustomerSummaryDto?>(null);

        public Task<IReadOnlyCollection<CustomerOrderDto>> GetRecentOrdersAsync(int customerId, int count = 5, CancellationToken cancellationToken = default)
        {
            GetRecentOrdersAsyncCalled = true;
            return Task.FromResult(RecentOrdersToReturn);
        }

        public Task<CustomerOrderSummaryDto?> GetOrderSummaryAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<CustomerOrderSummaryDto?>(null);
    }
}
