using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.IntegrationTests.Products;

/// <summary>
/// Exercises <see cref="ProductReadRepository"/> against the real local AdventureWorksLT
/// database. Read-only — uses stable seed rows and must not mutate any data.
///
/// Known seed data this file relies on (re-derive against the local database if it changes):
/// - ProductID 680 ("HL Road Frame - Black, 58", ProductNumber FR-R92B-58) exists with every
///   nullable column populated: Color Black, Size 58, Weight 1016.04, ProductCategoryID 18,
///   ProductModelID 6, SellStartDate 2002-06-01; SellEndDate and DiscontinuedDate are null.
/// - ProductID 879 ("All-Purpose Bike Stand") has null Color, Size, and Weight but a category
///   (31) and model (122) — the null-optional-columns mapping case.
/// - ProductID 999999 is unknown (max seeded ProductID is 999).
/// - No seeded product has a DiscontinuedDate, so IsDiscontinued's true branch is covered by
///   unit tests rather than seed data.
/// </summary>
public class ProductReadRepositoryTests
{
    private const int KnownProductId = 680;
    private const int KnownProductIdWithNullOptionals = 879;
    private const int UnknownProductId = 999999;

    private AdventureWorksLtDbContext _dbContext = null!;
    private ProductReadRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _repository = new ProductReadRepository(_dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // --- GetAllAsync -----------------------------------------------------------------------

    [Test]
    public async Task GetAllAsync_ReturnsProductsOrderedByName()
    {
        var products = await _repository.GetAllAsync();

        Assert.That(products, Is.Not.Empty);

        // Same reasoning as CustomerReadRepositoryTests: re-deriving the expected order with a
        // client-side OrderBy isn't reliable because SQL Server's collation doesn't match any
        // single .NET StringComparer once names contain punctuation ("HL Road Frame - Black,
        // 58"). Compare against an independent raw-SQL query using the same ORDER BY instead.
        var expectedOrder = await GetProductIdsOrderedByNameAsync();

        Assert.That(products.Select(p => p.ProductId).ToArray(), Is.EqualTo(expectedOrder));
    }

    private async Task<int[]> GetProductIdsOrderedByNameAsync()
    {
        const string sql = """
            SELECT ProductID
            FROM SalesLT.Product
            ORDER BY Name, ProductID;
            """;

        var connection = _dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();

            var productIds = new List<int>();

            while (await reader.ReadAsync())
            {
                productIds.Add(reader.GetInt32(0));
            }

            return productIds.ToArray();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    // --- GetByIdAsync ----------------------------------------------------------------------

    // Pins the entity mapping column-for-column against a fully-populated seed row, including
    // the money/decimal(8,2)/datetime columns that are this model's first non-string mappings.
    [Test]
    public async Task GetByIdAsync_KnownProduct_MapsEveryColumn()
    {
        var product = await _repository.GetByIdAsync(KnownProductId);

        Assert.That(product, Is.Not.Null);
        Assert.That(product!.ProductId, Is.EqualTo(680));
        Assert.That(product.Name, Is.EqualTo("HL Road Frame - Black, 58"));
        Assert.That(product.ProductNumber, Is.EqualTo("FR-R92B-58"));
        Assert.That(product.Color, Is.EqualTo("Black"));
        Assert.That(product.StandardCost, Is.EqualTo(1059.31m));
        Assert.That(product.ListPrice, Is.EqualTo(1431.50m));
        Assert.That(product.Size, Is.EqualTo("58"));
        Assert.That(product.Weight, Is.EqualTo(1016.04m));
        Assert.That(product.ProductCategoryId, Is.EqualTo(18));
        Assert.That(product.ProductModelId, Is.EqualTo(6));
        Assert.That(product.SellStartDate, Is.EqualTo(new DateTime(2002, 6, 1)));
        Assert.That(product.SellEndDate, Is.Null);
        Assert.That(product.DiscontinuedDate, Is.Null);
        Assert.That(product.IsDiscontinued, Is.False);
    }

    [Test]
    public async Task GetByIdAsync_ProductWithNullOptionalColumns_MapsNullsNotDefaults()
    {
        var product = await _repository.GetByIdAsync(KnownProductIdWithNullOptionals);

        Assert.That(product, Is.Not.Null);
        Assert.That(product!.Name, Is.EqualTo("All-Purpose Bike Stand"));
        Assert.That(product.Color, Is.Null);
        Assert.That(product.Size, Is.Null);
        Assert.That(product.Weight, Is.Null);
        Assert.That(product.ProductCategoryId, Is.EqualTo(31));
        Assert.That(product.ProductModelId, Is.EqualTo(122));
    }

    [Test]
    public async Task GetByIdAsync_UnknownProduct_ReturnsNull()
    {
        var product = await _repository.GetByIdAsync(UnknownProductId);

        Assert.That(product, Is.Null);
    }
}
