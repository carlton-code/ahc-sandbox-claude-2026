using AHC.Sandbox.Application.ProductModels.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

public class ProductModelWriteRepository : IProductModelWriteRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public ProductModelWriteRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<int>?> SetEnglishDescriptionAsync(
        int modelId,
        string description,
        CancellationToken cancellationToken = default)
    {
        var modelExists = await _dbContext.ProductModels
            .AnyAsync(m => m.ProductModelId == modelId, cancellationToken);

        if (!modelExists)
        {
            return null;
        }

        // Find the model's English description link (Culture is space-padded nchar(6), so LIKE
        // 'en%'). Updating the linked ProductDescription is safe and scoped: no ProductDescription
        // row is shared across mappings, so the edit affects only this model's English text.
        var link = await _dbContext.ProductModelProductDescriptions
            .FirstOrDefaultAsync(
                l => l.ProductModelId == modelId && EF.Functions.Like(l.Culture, "en%"),
                cancellationToken);

        if (link is not null)
        {
            var existing = await _dbContext.ProductDescriptionRows
                .FirstAsync(d => d.ProductDescriptionId == link.ProductDescriptionId, cancellationToken);

            existing.Description = description;
        }
        else
        {
            // The model has no English description yet: create the ProductDescription row (identity
            // id generated on the first save), then link it. rowguid/ModifiedDate on both tables
            // have database defaults, so they don't need to be set here.
            var newDescription = new ProductDescriptionEntity { Description = description };
            _dbContext.ProductDescriptionRows.Add(newDescription);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _dbContext.ProductModelProductDescriptions.Add(new ProductModelProductDescriptionEntity
            {
                ProductModelId = modelId,
                ProductDescriptionId = newDescription.ProductDescriptionId,
                Culture = "en"
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Every product on this model shares the description just changed, so each one's cached
        // read is now stale. Return their ids for the service to evict.
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.ProductModelId == modelId)
            .Select(p => p.ProductId)
            .ToArrayAsync(cancellationToken);
    }
}
