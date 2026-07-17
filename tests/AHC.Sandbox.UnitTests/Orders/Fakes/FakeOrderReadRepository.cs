using AHC.Sandbox.Application.Orders.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Orders.Fakes
{
    // Minimal hand-written in-memory fake for IOrderReadRepository, following
    // FakeProductReadRepository's pattern. Like the real repository, GetAllAsync returns the
    // orders exactly as stored — tests that care about Lines being empty on the list path
    // should add headers without lines.
    public class FakeOrderReadRepository : IOrderReadRepository
    {
        public List<Order> Orders { get; } = new();

        public Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Order>>(Orders);

        public Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult(Orders.FirstOrDefault(o => o.OrderId == orderId));
    }
}
