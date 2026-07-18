using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

public class ProductReadRepository : IProductReadRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public ProductReadRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ProductReadRows(_dbContext.Products.AsNoTracking())
            // Name is unique in SalesLT.Product, so this ordering is already deterministic;
            // ProductId is a tiebreaker only in case that constraint ever goes away.
            .OrderBy(r => r.Product.Name)
            .ThenBy(r => r.Product.ProductId)
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(ToDomain)
            .ToArray();
    }

    public async Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var row = await ProductReadRows(_dbContext.Products.AsNoTracking().Where(p => p.ProductId == productId))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToDomain(row);
    }

    // One query that resolves both read enrichments off the product:
    // - the English description via SalesLT.vProductAndDescription (LEFT JOIN, filtered to en);
    // - the category via SalesLT.ProductCategory, plus a self-join for the parent's name.
    // All LEFT JOINs: a product keeps coming back when it has no description, no category, or a
    // category that is itself a root (ParentName null).
    private IQueryable<ProductReadRow> ProductReadRows(IQueryable<ProductEntity> products)
        => from p in products
           join d in EnglishDescriptions()
               on p.ProductId equals d.ProductId into descriptions
           from d in descriptions.DefaultIfEmpty()
           join c in _dbContext.ProductCategories.AsNoTracking()
               on p.ProductCategoryId equals c.ProductCategoryId into categories
           from c in categories.DefaultIfEmpty()
           join parent in _dbContext.ProductCategories.AsNoTracking()
               on c.ParentProductCategoryId equals (int?)parent.ProductCategoryId into parents
           from parent in parents.DefaultIfEmpty()
           select new ProductReadRow
           {
               Product = p,
               Description = d != null ? d.Description : null,
               CategoryId = c != null ? (int?)c.ProductCategoryId : null,
               CategoryName = c != null ? c.Name : null,
               ParentCategoryName = parent != null ? parent.Name : null
           };

    private static Product ToDomain(ProductReadRow row)
        => ProductMapper.ToDomain(
            row.Product,
            row.Description,
            row.CategoryId is null
                ? null
                : new ProductCategory
                {
                    Id = row.CategoryId.Value,
                    Name = row.CategoryName ?? string.Empty,
                    ParentName = row.ParentCategoryName
                });

    private IQueryable<ProductDescriptionView> EnglishDescriptions()
        => _dbContext.ProductDescriptions
            .AsNoTracking()
            // Culture is nchar(6), so space-padded — LIKE 'en%', never = 'en'.
            .Where(v => EF.Functions.Like(v.Culture, "en%"));

    private sealed class ProductReadRow
    {
        public required ProductEntity Product { get; init; }
        public string? Description { get; init; }
        public int? CategoryId { get; init; }
        public string? CategoryName { get; init; }
        public string? ParentCategoryName { get; init; }
    }
}
