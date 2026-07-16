using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.Products.Interfaces
{
    public interface IProductWriteRepository
    {
        Task<Product> CreateAsync(CreateProductDto product, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(int productId, UpdateProductDto product, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default);
    }
}
