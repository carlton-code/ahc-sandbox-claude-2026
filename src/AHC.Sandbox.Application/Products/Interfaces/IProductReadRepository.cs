using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.Products.Interfaces
{
    public interface IProductReadRepository
    {
        Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
    }
}
