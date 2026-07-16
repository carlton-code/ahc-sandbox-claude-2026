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
        var entities = await _dbContext.Products
            .AsNoTracking()
            // Name is unique in SalesLT.Product, so this ordering is already deterministic;
            // ProductId is a tiebreaker only in case that constraint ever goes away.
            .OrderBy(p => p.Name)
            .ThenBy(p => p.ProductId)
            .ToArrayAsync(cancellationToken);

        return entities
            .Select(ProductMapper.ToDomain)
            .ToArray();
    }

    public async Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

        return entity is null ? null : ProductMapper.ToDomain(entity);
    }
}
