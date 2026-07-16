using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

public class ProductWriteRepository : IProductWriteRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public ProductWriteRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product> CreateAsync(CreateProductDto product, CancellationToken cancellationToken = default)
    {
        // The nullable value types are non-null by here: [Required] on the DTO already turned
        // an absent field into a 400 before the request reached this layer.
        var entity = new ProductEntity
        {
            Name = product.Name,
            ProductNumber = product.ProductNumber,
            Color = product.Color,
            StandardCost = product.StandardCost!.Value,
            ListPrice = product.ListPrice!.Value,
            Size = product.Size,
            Weight = product.Weight,
            ProductCategoryId = product.ProductCategoryId,
            ProductModelId = product.ProductModelId,
            SellStartDate = product.SellStartDate!.Value,
            SellEndDate = product.SellEndDate,
            DiscontinuedDate = product.DiscontinuedDate
        };

        _dbContext.Products.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToDomain(entity);
    }

    public async Task<bool> UpdateAsync(int productId, UpdateProductDto product, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.Name = product.Name;
        entity.ProductNumber = product.ProductNumber;
        entity.Color = product.Color;
        entity.StandardCost = product.StandardCost!.Value;
        entity.ListPrice = product.ListPrice!.Value;
        entity.Size = product.Size;
        entity.Weight = product.Weight;
        entity.ProductCategoryId = product.ProductCategoryId;
        entity.ProductModelId = product.ProductModelId;
        entity.SellStartDate = product.SellStartDate!.Value;
        entity.SellEndDate = product.SellEndDate;
        entity.DiscontinuedDate = product.DiscontinuedDate;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Products.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
