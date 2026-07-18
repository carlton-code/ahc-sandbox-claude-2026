using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.IntegrationTests.ProductModels;

/// <summary>
/// Exercises <see cref="ProductModelWriteRepository"/> against the real local AdventureWorksLT
/// database. This mutates shared seed data, so every test restores what it changed in
/// <see cref="TearDown"/> regardless of pass/fail:
/// - the update-path test captures the model's original English text and writes it back;
/// - the create-path test records the ProductDescription row it created and deletes it (plus its
///   ProductModelProductDescription link), so ProductModelID 128 goes back to having no English
///   description.
///
/// Known seed data:
/// - ProductModelID 6 ("HL Road Frame") has 11 products (including 680) and an English description.
/// - ProductModelID 128 ("Rear Brakes", product 907) has no English description — the create path.
/// </summary>
public class ProductModelWriteRepositoryTests
{
    private const int ModelWithEnglishDescription = 6;
    private const int ModelWithoutEnglishDescription = 128;
    private const int UnknownModelId = 999999;

    private AdventureWorksLtDbContext _dbContext = null!;
    private ProductModelReadRepository _readRepository = null!;
    private ProductModelWriteRepository _repository = null!;

    private (int ModelId, string OriginalDescription)? _descriptionToRestore;
    private int? _createdDescriptionId;

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _readRepository = new ProductModelReadRepository(_dbContext);
        _repository = new ProductModelWriteRepository(_dbContext);
        _descriptionToRestore = null;
        _createdDescriptionId = null;
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            if (_descriptionToRestore is { } restore)
            {
                await _repository.SetEnglishDescriptionAsync(restore.ModelId, restore.OriginalDescription);
            }

            if (_createdDescriptionId is { } descriptionId)
            {
                // Remove the link first (FK), then the description row, so ModelID 128 returns to
                // having no English description for the next run.
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM SalesLT.ProductModelProductDescription WHERE ProductModelID = {0} AND ProductDescriptionID = {1}",
                    ModelWithoutEnglishDescription,
                    descriptionId);
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM SalesLT.ProductDescription WHERE ProductDescriptionID = {0}",
                    descriptionId);
            }
        }
        finally
        {
            _dbContext.Dispose();
        }
    }

    [Test]
    public async Task SetEnglishDescriptionAsync_ExistingDescription_UpdatesTextAndReturnsAllProductsOnModel()
    {
        var before = await _readRepository.GetByIdAsync(ModelWithEnglishDescription);
        _descriptionToRestore = (ModelWithEnglishDescription, before!.Description!);

        var newText = $"Integration test description {Guid.NewGuid():N}";

        var affectedProductIds = await _repository.SetEnglishDescriptionAsync(ModelWithEnglishDescription, newText);

        Assert.That(affectedProductIds, Is.Not.Null);
        Assert.That(affectedProductIds!, Has.Count.EqualTo(11));
        Assert.That(affectedProductIds, Contains.Item(680));

        var after = await _readRepository.GetByIdAsync(ModelWithEnglishDescription);
        Assert.That(after!.Description, Is.EqualTo(newText));
    }

    [Test]
    public async Task SetEnglishDescriptionAsync_ModelWithNoDescription_CreatesItAndReturnsProducts()
    {
        var newText = $"Integration test description {Guid.NewGuid():N}";

        var affectedProductIds = await _repository.SetEnglishDescriptionAsync(ModelWithoutEnglishDescription, newText);

        Assert.That(affectedProductIds, Is.Not.Null);
        Assert.That(affectedProductIds, Contains.Item(907));

        var after = await _readRepository.GetByIdAsync(ModelWithoutEnglishDescription);
        Assert.That(after!.Description, Is.EqualTo(newText));

        // Record the created row so TearDown can delete it and restore the "no description" state.
        var createdLink = await _dbContext.ProductModelProductDescriptions
            .AsNoTracking()
            .FirstAsync(l => l.ProductModelId == ModelWithoutEnglishDescription && EF.Functions.Like(l.Culture, "en%"));
        _createdDescriptionId = createdLink.ProductDescriptionId;
    }

    [Test]
    public async Task SetEnglishDescriptionAsync_UnknownModel_ReturnsNull()
    {
        var result = await _repository.SetEnglishDescriptionAsync(UnknownModelId, "anything");

        Assert.That(result, Is.Null);
    }
}
