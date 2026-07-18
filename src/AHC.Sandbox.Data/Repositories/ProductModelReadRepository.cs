using AHC.Sandbox.Application.ProductModels.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

public class ProductModelReadRepository : IProductModelReadRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public ProductModelReadRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductModel?> GetByIdAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await ProjectModels(_dbContext.ProductModels.Where(m => m.ProductModelId == modelId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductModel?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        // int? projection: null if the product doesn't exist or has no model — either way there's
        // no model to return (404 at the API). All seeded products have a model, so in practice
        // this is null only for an unknown product.
        var modelId = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.ProductId == productId)
            .Select(p => p.ProductModelId)
            .FirstOrDefaultAsync(cancellationToken);

        return modelId is null ? null : await GetByIdAsync(modelId.Value, cancellationToken);
    }

    // Projects each model with its English description resolved through the
    // ProductModelProductDescription → ProductDescription join. The correlated subquery yields the
    // description or null (no English row), which keeps this a single query without a manual outer
    // join. Culture is nchar(6) — space-padded — so it's matched with LIKE 'en%', never = 'en'.
    private IQueryable<ProductModel> ProjectModels(IQueryable<ProductModelEntity> models)
    {
        return models
            .AsNoTracking()
            .Select(m => new ProductModel
            {
                ProductModelId = m.ProductModelId,
                Name = m.Name,
                Description = (from link in _dbContext.ProductModelProductDescriptions
                               join d in _dbContext.ProductDescriptionRows
                                   on link.ProductDescriptionId equals d.ProductDescriptionId
                               where link.ProductModelId == m.ProductModelId
                                     && EF.Functions.Like(link.Culture, "en%")
                               select d.Description).FirstOrDefault()
            });
    }
}
