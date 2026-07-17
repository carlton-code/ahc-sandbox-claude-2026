using AHC.Sandbox.Application.Orders.Dtos;

namespace AHC.Sandbox.Application.Orders.Interfaces
{
    public interface IOrderService
    {
        Task<IReadOnlyCollection<OrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default);
        Task<OrderWithLinesDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default);
    }
}
