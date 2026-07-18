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
        var rows = await (
            from p in _dbContext.Products.AsNoTracking()
            join d in EnglishDescriptions()
                on p.ProductId equals d.ProductId into descriptions
            from d in descriptions.DefaultIfEmpty()
            // Name is unique in SalesLT.Product, so this ordering is already deterministic;
            // ProductId is a tiebreaker only in case that constraint ever goes away.
            orderby p.Name, p.ProductId
            select new { Product = p, Description = d != null ? d.Description : null })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(r => ProductMapper.ToDomain(r.Product, r.Description))
            .ToArray();
    }

    public async Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from p in _dbContext.Products.AsNoTracking()
            where p.ProductId == productId
            join d in EnglishDescriptions()
                on p.ProductId equals d.ProductId into descriptions
            from d in descriptions.DefaultIfEmpty()
            select new { Product = p, Description = d != null ? d.Description : null })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ProductMapper.ToDomain(row.Product, row.Description);
    }

    // The description enrichment comes from SalesLT.vProductAndDescription, which carries one row
    // per product per culture. Filter to English before the LEFT JOIN (Culture is nchar(6), so
    // space-padded — LIKE 'en%', never = 'en'), and keep it a LEFT JOIN via DefaultIfEmpty so the
    // one seeded product with no English description still comes back (with a null description)
    // rather than being dropped.
    private IQueryable<ProductDescriptionView> EnglishDescriptions()
        => _dbContext.ProductDescriptions
            .AsNoTracking()
            .Where(v => EF.Functions.Like(v.Culture, "en%"));
}
