using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.Orders.Interfaces
{
    public interface IOrderReadRepository
    {
        /// <summary>
        /// Returns order headers only — <see cref="Order.Lines"/> is empty on every item.
        /// Loading 542 line rows to list 32 headers would be waste; the by-id read is the
        /// one that carries lines.
        /// A non-null <paramref name="customerId"/> filters to that customer's orders; an
        /// unknown customer (or one with no orders) yields an empty collection, not an error.
        /// </summary>
        Task<IReadOnlyCollection<Order>> GetAllAsync(int? customerId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the order with <see cref="Order.Lines"/> populated, or null if not found.
        /// </summary>
        Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
