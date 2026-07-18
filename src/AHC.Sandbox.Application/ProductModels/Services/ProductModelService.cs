using AHC.Sandbox.Application.Caching;
using AHC.Sandbox.Application.ProductModels.Dtos;
using AHC.Sandbox.Application.ProductModels.Interfaces;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AHC.Sandbox.Application.ProductModels.Services
{
    public class ProductModelService : IProductModelService
    {
        private readonly IProductModelReadRepository _readRepository;
        private readonly IProductModelWriteRepository _writeRepository;
        private readonly IProductCacheRepository _productCacheRepository;
        private readonly ILogger<ProductModelService> _logger;

        public ProductModelService(
            IProductModelReadRepository readRepository,
            IProductModelWriteRepository writeRepository,
            IProductCacheRepository productCacheRepository,
            ILogger<ProductModelService> logger)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _productCacheRepository = productCacheRepository;
            _logger = logger;
        }

        public async Task<ProductModelDto?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default)
        {
            var model = await _readRepository.GetByIdAsync(modelId, cancellationToken);

            return model is null ? null : MapModel(model);
        }

        public async Task<ProductModelDto?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var model = await _readRepository.GetByProductIdAsync(productId, cancellationToken);

            return model is null ? null : MapModel(model);
        }

        public async Task<bool> SetEnglishDescriptionAsync(
            int modelId,
            UpdateProductModelDescriptionDto request,
            CancellationToken cancellationToken = default)
        {
            // Description is non-null here: [Required] on the DTO turned an absent/whitespace value
            // into a 400 before the request reached this layer.
            var affectedProductIds = await _writeRepository.SetEnglishDescriptionAsync(
                modelId,
                request.Description!,
                cancellationToken);

            if (affectedProductIds is null)
            {
                return false;
            }

            // The description is a database-shared field: ProductDto.description is cached per
            // product, so every product on this model now holds a stale cached read. Evict each so
            // the next read repopulates from the database rather than serving the old text until
            // the entry's TTL expires.
            foreach (var productId in affectedProductIds)
            {
                await TryInvalidateProductCacheAsync(productId, cancellationToken);
            }

            return true;
        }

        private async Task TryInvalidateProductCacheAsync(int productId, CancellationToken cancellationToken)
        {
            try
            {
                await _productCacheRepository.RemoveAsync(productId, cancellationToken);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache invalidation failed for product {ProductId} after a model description edit; a stale cached read may be served until it expires.",
                    productId);
            }
        }

        private static ProductModelDto MapModel(ProductModel model)
        {
            return new ProductModelDto
            {
                ModelId = model.ProductModelId,
                Name = model.Name,
                Description = model.Description
            };
        }
    }
}
