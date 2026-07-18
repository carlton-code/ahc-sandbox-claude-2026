using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;

namespace AHC.Sandbox.IntegrationTests.ProductModels;

/// <summary>
/// Exercises <see cref="ProductModelReadRepository"/> against the real local AdventureWorksLT
/// database. Read-only — relies on stable seed rows and must not mutate any data.
///
/// Known seed data (re-derive if it changes):
/// - ProductModelID 6 ("HL Road Frame") has 11 products and an English description starting
///   "Our lightest and best quality aluminum frame".
/// - ProductModelID 128 ("Rear Brakes") has one product (ProductID 907) and NO English
///   description — its description resolves to null.
/// - ProductID 680 is on model 6.
/// </summary>
public class ProductModelReadRepositoryTests
{
    private const int ModelWithEnglishDescription = 6;
    private const int ModelWithoutEnglishDescription = 128;
    private const int KnownProductId = 680;
    private const int UnknownId = 999999;

    private AdventureWorksLtDbContext _dbContext = null!;
    private ProductModelReadRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _repository = new ProductModelReadRepository(_dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetByIdAsync_ModelWithEnglishDescription_PopulatesNameAndDescription()
    {
        var model = await _repository.GetByIdAsync(ModelWithEnglishDescription);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.ProductModelId, Is.EqualTo(6));
        Assert.That(model.Name, Is.EqualTo("HL Road Frame"));
        Assert.That(model.Description, Does.StartWith("Our lightest and best quality aluminum frame"));
    }

    [Test]
    public async Task GetByIdAsync_ModelWithoutEnglishDescription_ReturnsNullDescription()
    {
        var model = await _repository.GetByIdAsync(ModelWithoutEnglishDescription);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Name, Is.EqualTo("Rear Brakes"));
        Assert.That(model.Description, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_UnknownModel_ReturnsNull()
    {
        var model = await _repository.GetByIdAsync(UnknownId);

        Assert.That(model, Is.Null);
    }

    [Test]
    public async Task GetByProductIdAsync_KnownProduct_ReturnsItsModel()
    {
        var model = await _repository.GetByProductIdAsync(KnownProductId);

        Assert.That(model, Is.Not.Null);
        Assert.That(model!.ProductModelId, Is.EqualTo(ModelWithEnglishDescription));
        Assert.That(model.Name, Is.EqualTo("HL Road Frame"));
    }

    [Test]
    public async Task GetByProductIdAsync_UnknownProduct_ReturnsNull()
    {
        var model = await _repository.GetByProductIdAsync(UnknownId);

        Assert.That(model, Is.Null);
    }
}
