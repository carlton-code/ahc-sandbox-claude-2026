using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.ProductModels.Interfaces
{
    public interface IProductModelReadRepository
    {
        Task<ProductModel?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default);

        // Null when the product doesn't exist or has no model. All seeded products have a model,
        // so in practice this is null only for an unknown product.
        Task<ProductModel?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    }
}
