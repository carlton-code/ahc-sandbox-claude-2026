using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.IntegrationTests.Addresses;

/// <summary>
/// Exercises <see cref="AddressWriteRepository"/> against the real local AdventureWorksLT database.
///
/// Every test creates its own address rather than mutating a seeded one, and
/// <see cref="TearDown"/> removes whatever survived — the CustomerAddress link first, then the
/// Address row, since every foreign key here is NO_ACTION. <see cref="OneTimeTearDown"/> then
/// sweeps anything a killed test host or a crash between the insert and the id capture left
/// behind, matched on <see cref="TestAddressLine1"/>; without it a leaked link row would persist
/// and quietly break whichever fixture asserts on this customer's address count.
///
/// Known seed data:
/// - CustomerID 101 exists and has no address of its own, so an address created for it can't be
///   confused with seeded data. Deliberately *not* CustomerID 1: AddressReadRepositoryTests
///   asserts that customer has zero addresses, and a leak here would surface as a failure over
///   there.
/// - CustomerID 29503 has AddressID 541 ("Main Office") — a seeded, referenced address, used only
///   for the delete-refuses assertion and never mutated.
/// </summary>
public class AddressWriteRepositoryTests
{
    private const int CustomerIdWithoutAddresses = 101;
    private const int SeededReferencedAddressId = 541;
    private const int UnknownAddressId = 999999;

    // Marker that identifies rows this fixture created, for the crash-recovery sweep.
    private const string TestAddressLine1 = "1 Integration Test Way";

    // SQL error 547 — FOREIGN KEY constraint conflict. The same number
    // DatabaseConflictExceptionHandler maps to 409.
    private const int ForeignKeyViolation = 547;

    private AdventureWorksLtDbContext _dbContext = null!;
    private AddressReadRepository _readRepository = null!;
    private AddressWriteRepository _repository = null!;

    private int? _createdAddressId;

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _readRepository = new AddressReadRepository(_dbContext);
        _repository = new AddressWriteRepository(_dbContext);
        _createdAddressId = null;
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            if (_createdAddressId is { } addressId)
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM SalesLT.CustomerAddress WHERE AddressID = {0}",
                    addressId);
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "DELETE FROM SalesLT.Address WHERE AddressID = {0}",
                    addressId);
            }
        }
        finally
        {
            _dbContext.Dispose();
        }
    }

    // Safety net for the case per-test teardown can't cover: a killed test host, or a crash between
    // SaveChangesAsync succeeding and _createdAddressId being assigned. Matches on the marker line
    // rather than an id, and covers the updated variant too since UpdateAsync rewrites AddressLine1.
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        using var dbContext = DbContextTestFactory.Create();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE ca FROM SalesLT.CustomerAddress ca
            INNER JOIN SalesLT.Address a ON a.AddressID = ca.AddressID
            WHERE a.AddressLine1 LIKE {0};
            DELETE FROM SalesLT.Address WHERE AddressLine1 LIKE {0};
            """,
            "% Integration Test Way");
    }

    private static CreateCustomerAddressDto NewAddress(string addressType = "Shipping")
    {
        return new CreateCustomerAddressDto
        {
            AddressLine1 = TestAddressLine1,
            AddressLine2 = "Suite 100",
            City = "Seattle",
            StateProvince = "Washington",
            CountryRegion = "United States",
            PostalCode = "98104",
            AddressType = addressType
        };
    }

    private async Task<CustomerAddressDto> GivenCreatedAddressAsync(string addressType = "Shipping")
    {
        var created = await _repository.CreateForCustomerAsync(CustomerIdWithoutAddresses, NewAddress(addressType));
        _createdAddressId = created.AddressId;

        return created;
    }

    // --- CreateForCustomerAsync -----------------------------------------------------------------

    [Test]
    public async Task CreateForCustomerAsync_ReturnsMappedAddress_WithDatabaseGeneratedId()
    {
        var created = await GivenCreatedAddressAsync();

        Assert.Multiple(() =>
        {
            // AddressID is IDENTITY, so a non-zero id is proof EF read the generated key back
            // rather than the DTO echoing an unsaved default.
            Assert.That(created.AddressId, Is.GreaterThan(0));
            Assert.That(created.AddressLine1, Is.EqualTo("1 Integration Test Way"));
            Assert.That(created.AddressLine2, Is.EqualTo("Suite 100"));
            Assert.That(created.City, Is.EqualTo("Seattle"));
            Assert.That(created.StateProvince, Is.EqualTo("Washington"));
            Assert.That(created.CountryRegion, Is.EqualTo("United States"));
            Assert.That(created.PostalCode, Is.EqualTo("98104"));
            Assert.That(created.AddressType, Is.EqualTo("Shipping"));
            Assert.That(
                created.SingleLineAddress,
                Is.EqualTo("1 Integration Test Way, Suite 100, Seattle, Washington, 98104, United States"));
        });
    }

    // The whole reason create is nested under a customer: the Address row alone would be an orphan.
    // Reading it back through the customer proves the CustomerAddress link row was written too, and
    // that EF fixed the generated AddressID into its foreign key.
    [Test]
    public async Task CreateForCustomerAsync_LinksAddressToCustomer()
    {
        var created = await GivenCreatedAddressAsync();

        var linked = await _readRepository.GetByCustomerAndAddressIdAsync(
            CustomerIdWithoutAddresses,
            created.AddressId);

        Assert.That(linked, Is.Not.Null);
        Assert.That(linked!.AddressType, Is.EqualTo("Shipping"));
    }

    [Test]
    public async Task CreateForCustomerAsync_AddressIsReadableById()
    {
        var created = await GivenCreatedAddressAsync();

        var address = await _readRepository.GetByIdAsync(created.AddressId);

        Assert.That(address, Is.Not.Null);
        Assert.That(address!.AddressLine1, Is.EqualTo("1 Integration Test Way"));
    }

    // --- UpdateAsync ----------------------------------------------------------------------------

    [Test]
    public async Task UpdateAsync_ReplacesFieldsAndReturnsTrue()
    {
        var created = await GivenCreatedAddressAsync();

        var updated = await _repository.UpdateAsync(created.AddressId, new UpdateAddressDto
        {
            AddressLine1 = "2 Integration Test Way",
            AddressLine2 = null,
            City = "Portland",
            StateProvince = "Oregon",
            CountryRegion = "United States",
            PostalCode = "97204"
        });

        var address = await _readRepository.GetByIdAsync(created.AddressId);

        Assert.That(updated, Is.True);
        Assert.That(address, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(address!.AddressLine1, Is.EqualTo("2 Integration Test Way"));
            // PUT replaces the whole record, so an omitted AddressLine2 really is cleared — that's
            // the difference from a PATCH, which this resource deliberately doesn't have.
            Assert.That(address.AddressLine2, Is.Null);
            Assert.That(address.City, Is.EqualTo("Portland"));
            Assert.That(address.StateProvince, Is.EqualTo("Oregon"));
            Assert.That(address.PostalCode, Is.EqualTo("97204"));
        });
    }

    // Updating the address never touches the link row, so the address type is untouched by a PUT.
    [Test]
    public async Task UpdateAsync_LeavesAddressTypeUnchanged()
    {
        var created = await GivenCreatedAddressAsync("Main Office");

        await _repository.UpdateAsync(created.AddressId, new UpdateAddressDto
        {
            AddressLine1 = "2 Integration Test Way",
            City = "Portland",
            StateProvince = "Oregon",
            CountryRegion = "United States",
            PostalCode = "97204"
        });

        var linked = await _readRepository.GetByCustomerAndAddressIdAsync(
            CustomerIdWithoutAddresses,
            created.AddressId);

        Assert.That(linked!.AddressType, Is.EqualTo("Main Office"));
    }

    [Test]
    public async Task UpdateAsync_ReturnsFalse_WhenAddressDoesNotExist()
    {
        var updated = await _repository.UpdateAsync(UnknownAddressId, new UpdateAddressDto
        {
            AddressLine1 = "2 Integration Test Way",
            City = "Portland",
            StateProvince = "Oregon",
            CountryRegion = "United States",
            PostalCode = "97204"
        });

        Assert.That(updated, Is.False);
    }

    // --- DeleteAsync ----------------------------------------------------------------------------

    // The case that actually happens. Every address reachable through the API has a CustomerAddress
    // link (creating one always writes it, and all 450 seeded addresses have one), and every foreign
    // key in this database is NO_ACTION — so DELETE always lands here. Asserting the failure rather
    // than the success is the lesson of ADR-0009, where the suite proved the one delete that worked
    // and never touched the 847 that didn't.
    [Test]
    public async Task DeleteAsync_Throws547_WhenACustomerStillLinksTheAddress()
    {
        var created = await GivenCreatedAddressAsync();

        var exception = Assert.ThrowsAsync<DbUpdateException>(
            async () => await _repository.DeleteAsync(created.AddressId));

        Assert.That(exception!.InnerException, Is.TypeOf<SqlException>());
        Assert.That(((SqlException)exception.InnerException!).Number, Is.EqualTo(ForeignKeyViolation));
    }

    [Test]
    public void DeleteAsync_Throws547_ForASeededReferencedAddress()
    {
        var exception = Assert.ThrowsAsync<DbUpdateException>(
            async () => await _repository.DeleteAsync(SeededReferencedAddressId));

        Assert.That(exception!.InnerException, Is.TypeOf<SqlException>());
        Assert.That(((SqlException)exception.InnerException!).Number, Is.EqualTo(ForeignKeyViolation));
    }

    // The success path exists on the repository but no API caller can reach it, since POST always
    // writes a link row. Unlinking by hand first is the only way to exercise it — worth doing so the
    // Remove/SaveChanges path is proven, but see
    // docs/adr/0013-address-writes-split-across-two-route-prefixes.md for why 204 never surfaces.
    [Test]
    public async Task DeleteAsync_ReturnsTrue_WhenNothingReferencesTheAddress()
    {
        var created = await GivenCreatedAddressAsync();

        await _dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM SalesLT.CustomerAddress WHERE AddressID = {0}",
            created.AddressId);

        var deleted = await _repository.DeleteAsync(created.AddressId);

        Assert.That(deleted, Is.True);
        Assert.That(await _readRepository.GetByIdAsync(created.AddressId), Is.Null);
    }

    [Test]
    public async Task DeleteAsync_ReturnsFalse_WhenAddressDoesNotExist()
    {
        var deleted = await _repository.DeleteAsync(UnknownAddressId);

        Assert.That(deleted, Is.False);
    }
}
