using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.Orders.Interfaces
{
    public interface IOrderReadRepository
    {
        /// <summary>
        /// Returns order headers only — <see cref="Order.Lines"/> is empty on every item.
        /// Loading 542 line rows to list 32 headers would be waste; the by-id read is the
        /// one that carries lines.
        /// </summary>
        Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the order with <see cref="Order.Lines"/> populated, or null if not found.
        /// </summary>
        Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
