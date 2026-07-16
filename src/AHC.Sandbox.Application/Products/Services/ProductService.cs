using AHC.Sandbox.Application.Caching;
using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AHC.Sandbox.Application.Products.Services
{

    public class ProductService : IProductService
    {
        private readonly IProductReadRepository _productReadRepository;
        private readonly IProductWriteRepository _productWriteRepository;
        private readonly IProductCacheRepository _productCacheRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductReadRepository productReadRepository,
            IProductWriteRepository productWriteRepository,
            IProductCacheRepository productCacheRepository,
            ILogger<ProductService> logger)
        {
            _productReadRepository = productReadRepository;
            _productWriteRepository = productWriteRepository;
            _productCacheRepository = productCacheRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            var products = await _productReadRepository.GetAllAsync(cancellationToken);

            return products
                .Select(MapProduct)
                .ToArray();
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            var (cacheAvailable, cached) = await TryGetCachedProductAsync(productId, cancellationToken);

            if (cached is not null)
            {
                return cached;
            }

            var product = await _productReadRepository.GetByIdAsync(productId, cancellationToken);

            if (product is null)
            {
                return null;
            }

            var productDto = MapProduct(product);

            // Skip the cache-populate attempt if the read above already showed Redis is
            // unreachable — trying again milliseconds later within the same request would
            // almost certainly fail too, doubling this request's latency for no benefit.
            if (cacheAvailable)
            {
                await TryCacheProductAsync(productId, productDto, cancellationToken);
            }

            return productDto;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto product, CancellationToken cancellationToken = default)
        {
            var createdProduct = await _productWriteRepository.CreateAsync(product, cancellationToken);

            return MapProduct(createdProduct);
        }

        public async Task<bool> UpdateProductAsync(int productId, UpdateProductDto product, CancellationToken cancellationToken = default)
        {
            var updated = await _productWriteRepository.UpdateAsync(productId, product, cancellationToken);

            if (updated)
            {
                await TryInvalidateCacheAsync(productId, cancellationToken);
            }

            return updated;
        }

        public async Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var deleted = await _productWriteRepository.DeleteAsync(productId, cancellationToken);

            if (deleted)
            {
                await TryInvalidateCacheAsync(productId, cancellationToken);
            }

            return deleted;
        }

        private async Task<(bool CacheAvailable, ProductDto? Product)> TryGetCachedProductAsync(int productId, CancellationToken cancellationToken)
        {
            try
            {
                var product = await _productCacheRepository.GetByIdAsync(productId, cancellationToken);

                return (true, product);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogWarning(ex, "Redis cache read failed for product {ProductId}; falling back to database.", productId);

                return (false, null);
            }
        }

        private async Task TryCacheProductAsync(int productId, ProductDto product, CancellationToken cancellationToken)
        {
            try
            {
                await _productCacheRepository.SetAsync(productId, product, cancellationToken);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogWarning(ex, "Redis cache write failed for product {ProductId}; continuing without caching this read.", productId);
            }
        }

        private async Task TryInvalidateCacheAsync(int productId, CancellationToken cancellationToken)
        {
            try
            {
                await _productCacheRepository.RemoveAsync(productId, cancellationToken);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogWarning(ex, "Redis cache invalidation failed for product {ProductId}; a stale cached read may be served until it expires.", productId);
            }
        }

        private static ProductDto MapProduct(Product product)
        {
            return new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                ProductNumber = product.ProductNumber,
                Color = product.Color,
                StandardCost = product.StandardCost,
                ListPrice = product.ListPrice,
                Size = product.Size,
                Weight = product.Weight,
                ProductCategoryId = product.ProductCategoryId,
                ProductModelId = product.ProductModelId,
                SellStartDate = product.SellStartDate,
                SellEndDate = product.SellEndDate,
                DiscontinuedDate = product.DiscontinuedDate,
                IsDiscontinued = product.IsDiscontinued
            };
        }
    }

}
