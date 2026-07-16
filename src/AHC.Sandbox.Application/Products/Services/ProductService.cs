using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.Products.Services
{

    public class ProductService : IProductService
    {
        private readonly IProductReadRepository _productReadRepository;
        private readonly IProductWriteRepository _productWriteRepository;

        public ProductService(
            IProductReadRepository productReadRepository,
            IProductWriteRepository productWriteRepository)
        {
            _productReadRepository = productReadRepository;
            _productWriteRepository = productWriteRepository;
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
            var product = await _productReadRepository.GetByIdAsync(productId, cancellationToken);

            return product is null ? null : MapProduct(product);
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto product, CancellationToken cancellationToken = default)
        {
            var createdProduct = await _productWriteRepository.CreateAsync(product, cancellationToken);

            return MapProduct(createdProduct);
        }

        public Task<bool> UpdateProductAsync(int productId, UpdateProductDto product, CancellationToken cancellationToken = default)
        {
            return _productWriteRepository.UpdateAsync(productId, product, cancellationToken);
        }

        public Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            return _productWriteRepository.DeleteAsync(productId, cancellationToken);
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
