using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Data.Repositories;

/// <summary>
/// Shared <c>ProductEntity</c> → <c>Product</c> mapping for the read and write repositories, so
/// a new column gets added in one place instead of drifting between two private copies.
/// </summary>
internal static class ProductMapper
{
    internal static Product ToDomain(ProductEntity entity)
    {
        return new Product
        {
            ProductId = entity.ProductId,
            Name = entity.Name,
            ProductNumber = entity.ProductNumber,
            Color = entity.Color,
            StandardCost = entity.StandardCost,
            ListPrice = entity.ListPrice,
            Size = entity.Size,
            Weight = entity.Weight,
            ProductCategoryId = entity.ProductCategoryId,
            ProductModelId = entity.ProductModelId,
            SellStartDate = entity.SellStartDate,
            SellEndDate = entity.SellEndDate,
            DiscontinuedDate = entity.DiscontinuedDate
        };
    }
}
