using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Data.Repositories;

/// <summary>
/// Shared <c>ProductEntity</c> → <c>Product</c> mapping for the read and write repositories, so
/// a new column gets added in one place instead of drifting between two private copies.
/// </summary>
internal static class ProductMapper
{
    /// <param name="description">
    /// English description from <c>SalesLT.vProductAndDescription</c>, or null when the product has
    /// none. The write path doesn't read the view, so it leaves this null.
    /// </param>
    /// <param name="category">
    /// Resolved category (name + parent name) from <c>SalesLT.ProductCategory</c>, or null when the
    /// product has no category. Read path only — the write path leaves this null.
    /// </param>
    internal static Product ToDomain(ProductEntity entity, string? description = null, ProductCategory? category = null)
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
            DiscontinuedDate = entity.DiscontinuedDate,
            Description = description,
            Category = category
        };
    }
}
