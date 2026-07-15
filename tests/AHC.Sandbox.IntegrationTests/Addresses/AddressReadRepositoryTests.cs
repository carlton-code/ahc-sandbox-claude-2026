using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;

namespace AHC.Sandbox.IntegrationTests.Addresses;

/// <summary>
/// Exercises <see cref="AddressReadRepository"/> against the real local AdventureWorksLT
/// database. Read-only — uses stable seed rows and must not mutate any data.
///
/// Known seed data this file relies on (see <c>docs/database-schema.md</c> for how to re-derive
/// these if the local database changes):
/// - CustomerID 29503 has exactly two addresses: AddressID 541 ("Main Office", 25981 College
///   Street) and AddressID 32 ("Shipping", 26910 Indela Road), both in Montreal, Quebec, Canada.
///   Only 10 of 847 customers have more than one address, and each of those has one of each type.
/// - CustomerID 1 (Orlando Gee) exists but has no address at all. That's the majority state:
///   440 of 847 customers have none.
/// - CustomerID 999999 is unknown (max CustomerID in the seed data is 30118).
/// - AddressID 541 is linked to 29503 and not to CustomerID 1, giving a customer/address
///   mismatch pair.
/// </summary>
public class AddressReadRepositoryTests
{
    private const int KnownCustomerIdWithTwoAddresses = 29503;
    private const int KnownCustomerIdWithoutAddresses = 1;
    private const int UnknownCustomerId = 999999;
    private const int MainOfficeAddressId = 541;
    private const int ShippingAddressId = 32;
    private const int UnknownAddressId = 999999;

    private AdventureWorksLtDbContext _dbContext = null!;
    private AddressReadRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _repository = new AddressReadRepository(_dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    // --- GetByCustomerIdAsync ------------------------------------------------------------------

    [Test]
    public async Task GetByCustomerIdAsync_ReturnsBothAddresses_ForCustomerWithTwo()
    {
        var addresses = await _repository.GetByCustomerIdAsync(KnownCustomerIdWithTwoAddresses);

        Assert.That(addresses.Select(a => a.AddressId), Is.EqualTo(new[] { MainOfficeAddressId, ShippingAddressId }));
        Assert.That(addresses.Select(a => a.AddressType), Is.EqualTo(new[] { "Main Office", "Shipping" }));
    }

    // The ordering is AddressType then AddressId. This customer proves it's really doing that:
    // "Main Office" is AddressID 541 and "Shipping" is AddressID 32, so ordering by AddressId
    // alone would put them the other way round.
    [Test]
    public async Task GetByCustomerIdAsync_OrdersByAddressType_NotAddressId()
    {
        var addresses = await _repository.GetByCustomerIdAsync(KnownCustomerIdWithTwoAddresses);

        Assert.That(addresses.First().AddressId, Is.EqualTo(MainOfficeAddressId));
        Assert.That(addresses.First().AddressId, Is.GreaterThan(addresses.Last().AddressId));
    }

    [Test]
    public async Task GetByCustomerIdAsync_MapsAllAddressFields()
    {
        var addresses = await _repository.GetByCustomerIdAsync(KnownCustomerIdWithTwoAddresses);

        var mainOffice = addresses.Single(a => a.AddressId == MainOfficeAddressId);

        Assert.Multiple(() =>
        {
            Assert.That(mainOffice.AddressLine1, Is.EqualTo("25981 College Street"));
            Assert.That(mainOffice.AddressLine2, Is.Null);
            Assert.That(mainOffice.City, Is.EqualTo("Montreal"));
            Assert.That(mainOffice.StateProvince, Is.EqualTo("Quebec"));
            Assert.That(mainOffice.CountryRegion, Is.EqualTo("Canada"));
            Assert.That(mainOffice.PostalCode, Is.EqualTo("H1Y 2H5"));
            Assert.That(mainOffice.AddressType, Is.EqualTo("Main Office"));
            Assert.That(
                mainOffice.SingleLineAddress,
                Is.EqualTo("25981 College Street, Montreal, Quebec, H1Y 2H5, Canada"));
        });
    }

    [Test]
    public async Task GetByCustomerIdAsync_ReturnsEmpty_ForCustomerWithNoAddresses()
    {
        var addresses = await _repository.GetByCustomerIdAsync(KnownCustomerIdWithoutAddresses);

        Assert.That(addresses, Is.Empty);
    }

    // The repository can't distinguish an unknown customer from one with no addresses — both are
    // zero rows. AddressService is what turns the former into a 404; see AddressServiceTests.
    [Test]
    public async Task GetByCustomerIdAsync_ReturnsEmpty_ForUnknownCustomer()
    {
        var addresses = await _repository.GetByCustomerIdAsync(UnknownCustomerId);

        Assert.That(addresses, Is.Empty);
    }

    // --- GetByCustomerAndAddressIdAsync --------------------------------------------------------

    [Test]
    public async Task GetByCustomerAndAddressIdAsync_ReturnsAddress_WhenLinkedToCustomer()
    {
        var address = await _repository.GetByCustomerAndAddressIdAsync(
            KnownCustomerIdWithTwoAddresses,
            ShippingAddressId);

        Assert.That(address, Is.Not.Null);
        Assert.That(address!.AddressId, Is.EqualTo(ShippingAddressId));
        Assert.That(address.AddressType, Is.EqualTo("Shipping"));
        Assert.That(address.AddressLine1, Is.EqualTo("26910 Indela Road"));
    }

    // AddressID 541 is a real address — just not this customer's. Scoping by CustomerID is what
    // stops one customer reading another's address by guessing an id.
    [Test]
    public async Task GetByCustomerAndAddressIdAsync_ReturnsNull_WhenAddressBelongsToAnotherCustomer()
    {
        var address = await _repository.GetByCustomerAndAddressIdAsync(
            KnownCustomerIdWithoutAddresses,
            MainOfficeAddressId);

        Assert.That(address, Is.Null);
    }

    [Test]
    public async Task GetByCustomerAndAddressIdAsync_ReturnsNull_WhenAddressDoesNotExist()
    {
        var address = await _repository.GetByCustomerAndAddressIdAsync(
            KnownCustomerIdWithTwoAddresses,
            UnknownAddressId);

        Assert.That(address, Is.Null);
    }
}
