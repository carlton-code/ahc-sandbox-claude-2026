using AHC.Sandbox.Application.Products.Dtos;

namespace AHC.Sandbox.Application.Products.Interfaces
{
    public interface IProductCacheRepository
    {
        Task<ProductDto?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
        Task SetAsync(int productId, ProductDto product, CancellationToken cancellationToken = default);
        Task RemoveAsync(int productId, CancellationToken cancellationToken = default);
    }
}
