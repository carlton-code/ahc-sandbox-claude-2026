using AHC.Sandbox.Application.ProductModels.Dtos;

namespace AHC.Sandbox.Application.ProductModels.Interfaces
{
    public interface IProductModelService
    {
        Task<ProductModelDto?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default);
        Task<ProductModelDto?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

        // False when no model with that id exists (→ 404). True on a successful create-or-update.
        Task<bool> SetEnglishDescriptionAsync(
            int modelId,
            UpdateProductModelDescriptionDto request,
            CancellationToken cancellationToken = default);
    }
}
