using AHC.Sandbox.Application.ProductModels.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.ProductModels.Fakes
{
    // Minimal in-memory fake for IProductModelReadRepository. Each lookup returns its own preset
    // value so a test can drive the found / not-found branches independently.
    public class FakeProductModelReadRepository : IProductModelReadRepository
    {
        public ProductModel? ModelById { get; set; }
        public ProductModel? ModelByProductId { get; set; }

        public int? LastRequestedModelId { get; private set; }
        public int? LastRequestedProductId { get; private set; }

        public Task<ProductModel?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default)
        {
            LastRequestedModelId = modelId;
            return Task.FromResult(ModelById);
        }

        public Task<ProductModel?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            LastRequestedProductId = productId;
            return Task.FromResult(ModelByProductId);
        }
    }
}
