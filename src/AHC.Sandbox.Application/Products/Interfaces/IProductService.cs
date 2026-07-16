using AHC.Sandbox.Application.Products.Dtos;

namespace AHC.Sandbox.Application.Products.Interfaces
{
    public interface IProductService
    {
        Task<IReadOnlyCollection<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
        Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);
        Task<ProductDto> CreateProductAsync(CreateProductDto product, CancellationToken cancellationToken = default);
        Task<bool> UpdateProductAsync(int productId, UpdateProductDto product, CancellationToken cancellationToken = default);
        Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default);
    }
}
