using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Products.Fakes
{
    // Minimal hand-written in-memory fake for IProductWriteRepository. Records the last request
    // passed to each method so tests can assert on exactly what ProductService sent, and exposes
    // settable results so tests can drive both the success and failure paths.
    public class FakeProductWriteRepository : IProductWriteRepository
    {
        public Product ProductToReturnOnCreate { get; set; } = new();
        public bool UpdateResult { get; set; } = true;
        public bool DeleteResult { get; set; } = true;

        public CreateProductDto? LastCreateRequest { get; private set; }
        public (int ProductId, UpdateProductDto Dto)? LastUpdateRequest { get; private set; }
        public int? LastDeleteRequest { get; private set; }

        public Task<Product> CreateAsync(CreateProductDto product, CancellationToken cancellationToken = default)
        {
            LastCreateRequest = product;
            return Task.FromResult(ProductToReturnOnCreate);
        }

        public Task<bool> UpdateAsync(int productId, UpdateProductDto product, CancellationToken cancellationToken = default)
        {
            LastUpdateRequest = (productId, product);
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
        {
            LastDeleteRequest = productId;
            return Task.FromResult(DeleteResult);
        }
    }
}
