using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.IntegrationTests.Products;

/// <summary>
/// Exercises <see cref="ProductWriteRepository"/> against the real local AdventureWorksLT
/// database. This is a shared dev database, not a disposable container: every test that creates
/// a row uses unique <see cref="Guid"/>-suffixed Name/ProductNumber values (both columns are
/// unique in <c>SalesLT.Product</c>) and deletes what it created in <see cref="TearDown"/>,
/// regardless of pass/fail.
/// </summary>
public class ProductWriteRepositoryTests
{
    // Sport-100 Helmet, Red — referenced by seven SalesLT.SalesOrderDetail rows, and the FK is
    // NO_ACTION, so it is not deletable. The API turns that into a 409 via
    // DatabaseConflictExceptionHandler; at this layer it's still the raw EF exception.
    private const int KnownSeededProductIdWithOrderLines = 707;

    private AdventureWorksLtDbContext _dbContext = null!;
    private ProductWriteRepository _repository = null!;
    private readonly List<int> _createdProductIds = new();

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _repository = new ProductWriteRepository(_dbContext);
    }

    [TearDown]
    public async Task TearDown()
    {
        foreach (var productId in _createdProductIds)
        {
            try
            {
                await _repository.DeleteAsync(productId);
            }
            catch
            {
                // Best-effort cleanup: if delete also fails there's nothing further this test
                // teardown can do, and swallowing here must not mask the original test failure.
            }
        }

        _createdProductIds.Clear();
        _dbContext.Dispose();
    }

    // Name is nvarchar(50) and ProductNumber nvarchar(25), both unique — keep generated values
    // unique per test run and well under the column widths.
    private static CreateProductDto BuildCreateDto()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];

        return new CreateProductDto
        {
            Name = $"Integration Test Product {suffix}",
            ProductNumber = $"IT-{suffix}",
            Color = "Blue",
            StandardCost = 10.50m,
            ListPrice = 19.99m,
            Size = "M",
            Weight = 1.25m,
            SellStartDate = new DateTime(2026, 1, 1)
        };
    }

    // --- CreateAsync -----------------------------------------------------------------------

    [Test]
    public async Task CreateAsync_InsertsProduct_AndReadBackConfirmsPersistence()
    {
        var createDto = BuildCreateDto();

        var created = await _repository.CreateAsync(createDto);
        _createdProductIds.Add(created.ProductId);

        Assert.That(created.ProductId, Is.GreaterThan(0));
        Assert.That(created.Name, Is.EqualTo(createDto.Name));
        Assert.That(created.ProductNumber, Is.EqualTo(createDto.ProductNumber));

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new ProductReadRepository(verifyContext);
        var readBack = await readRepository.GetByIdAsync(created.ProductId);

        Assert.That(readBack, Is.Not.Null);
        Assert.That(readBack!.Name, Is.EqualTo(createDto.Name));
        Assert.That(readBack.ProductNumber, Is.EqualTo(createDto.ProductNumber));
        Assert.That(readBack.Color, Is.EqualTo("Blue"));

        // The money/decimal(8,2)/datetime columns must round-trip exactly — a wrong store type
        // (e.g. EF's default decimal(18,2)) would surface here as silent truncation or drift.
        Assert.That(readBack.StandardCost, Is.EqualTo(10.50m));
        Assert.That(readBack.ListPrice, Is.EqualTo(19.99m));
        Assert.That(readBack.Weight, Is.EqualTo(1.25m));
        Assert.That(readBack.SellStartDate, Is.EqualTo(new DateTime(2026, 1, 1)));

        // The nullable FKs were not set, and must stay null rather than defaulting to 0.
        Assert.That(readBack.ProductCategoryId, Is.Null);
        Assert.That(readBack.ProductModelId, Is.Null);
        Assert.That(readBack.SellEndDate, Is.Null);
        Assert.That(readBack.DiscontinuedDate, Is.Null);
    }

    // ProductNumber is unique (AK_Product_ProductNumber) — the source of the API's 409 on POST.
    // The failing create uses its own context: a failed SaveChanges leaves the entity tracked as
    // Added, which would poison this fixture's shared context and break TearDown's cleanup.
    [Test]
    public async Task CreateAsync_DuplicateProductNumber_ThrowsRatherThanInserting()
    {
        var original = BuildCreateDto();
        var created = await _repository.CreateAsync(original);
        _createdProductIds.Add(created.ProductId);

        var duplicate = BuildCreateDto();
        var duplicateWithSameNumber = new CreateProductDto
        {
            Name = duplicate.Name,
            ProductNumber = original.ProductNumber,
            StandardCost = duplicate.StandardCost,
            ListPrice = duplicate.ListPrice,
            SellStartDate = duplicate.SellStartDate
        };

        using var duplicateContext = DbContextTestFactory.Create();
        var duplicateRepository = new ProductWriteRepository(duplicateContext);

        Assert.ThrowsAsync<DbUpdateException>(async () =>
            await duplicateRepository.CreateAsync(duplicateWithSameNumber));
    }

    // --- UpdateAsync -----------------------------------------------------------------------

    [Test]
    public async Task UpdateAsync_ExistingProduct_UpdatesFieldsAndReturnsTrue()
    {
        var created = await _repository.CreateAsync(BuildCreateDto());
        _createdProductIds.Add(created.ProductId);

        var suffix = Guid.NewGuid().ToString("N")[..12];
        var updateDto = new UpdateProductDto
        {
            Name = $"Updated Test Product {suffix}",
            ProductNumber = $"IT-U-{suffix}",
            Color = "Red",
            StandardCost = 11.00m,
            ListPrice = 24.99m,
            Size = "L",
            Weight = 1.50m,
            SellStartDate = new DateTime(2026, 1, 1),
            SellEndDate = new DateTime(2026, 6, 30),
            DiscontinuedDate = new DateTime(2026, 7, 1)
        };

        var result = await _repository.UpdateAsync(created.ProductId, updateDto);

        Assert.That(result, Is.True);

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new ProductReadRepository(verifyContext);
        var updated = await readRepository.GetByIdAsync(created.ProductId);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Name, Is.EqualTo(updateDto.Name));
        Assert.That(updated.ProductNumber, Is.EqualTo(updateDto.ProductNumber));
        Assert.That(updated.Color, Is.EqualTo("Red"));
        Assert.That(updated.StandardCost, Is.EqualTo(11.00m));
        Assert.That(updated.ListPrice, Is.EqualTo(24.99m));
        Assert.That(updated.Size, Is.EqualTo("L"));
        Assert.That(updated.Weight, Is.EqualTo(1.50m));
        Assert.That(updated.SellEndDate, Is.EqualTo(new DateTime(2026, 6, 30)));
        Assert.That(updated.DiscontinuedDate, Is.EqualTo(new DateTime(2026, 7, 1)));
        Assert.That(updated.IsDiscontinued, Is.True);
    }

    [Test]
    public async Task UpdateAsync_UnknownProduct_ReturnsFalse()
    {
        var updateDto = new UpdateProductDto
        {
            Name = "Ghost Product",
            ProductNumber = "IT-GHOST",
            StandardCost = 1m,
            ListPrice = 2m,
            SellStartDate = new DateTime(2026, 1, 1)
        };

        var result = await _repository.UpdateAsync(999999, updateDto);

        Assert.That(result, Is.False);
    }

    // --- DeleteAsync -----------------------------------------------------------------------

    [Test]
    public async Task DeleteAsync_ExistingProduct_RemovesRowAndReturnsTrue()
    {
        var created = await _repository.CreateAsync(BuildCreateDto());
        _createdProductIds.Add(created.ProductId);

        var result = await _repository.DeleteAsync(created.ProductId);

        Assert.That(result, Is.True);

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new ProductReadRepository(verifyContext);
        var afterDelete = await readRepository.GetByIdAsync(created.ProductId);

        Assert.That(afterDelete, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_UnknownProduct_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(999999);

        Assert.That(result, Is.False);
    }

    // Mirrors CustomerWriteRepositoryTests' delete-with-dependents case: every other delete test
    // uses a product created seconds earlier, the only kind with no dependent rows. A seeded
    // product referenced by order lines must throw rather than delete.
    [Test]
    public void DeleteAsync_SeededProductWithOrderLines_ThrowsRatherThanDeleting()
    {
        Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _repository.DeleteAsync(KnownSeededProductIdWithOrderLines));
    }

    [Test]
    public async Task DeleteAsync_SeededProductWithOrderLines_LeavesTheProductIntact()
    {
        try
        {
            await _repository.DeleteAsync(KnownSeededProductIdWithOrderLines);
        }
        catch (DbUpdateException)
        {
            // Expected — asserted in the test above. What matters here is the row surviving.
        }

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new ProductReadRepository(verifyContext);
        var stillThere = await readRepository.GetByIdAsync(KnownSeededProductIdWithOrderLines);

        Assert.That(stillThere, Is.Not.Null);
    }
}
