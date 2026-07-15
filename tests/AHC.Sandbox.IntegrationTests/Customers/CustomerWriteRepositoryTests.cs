using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.IntegrationTests.Customers;

/// <summary>
/// Exercises <see cref="CustomerWriteRepository"/> against the real local AdventureWorksLT
/// database. This is a shared dev database, not a disposable container: every test that creates
/// a row uses a unique <see cref="Guid"/>-suffixed email and deletes what it created in
/// <see cref="TearDown"/>, regardless of pass/fail.
/// </summary>
public class CustomerWriteRepositoryTests
{
    // Orlando Gee. Has no address, no order and no rewards tier, but does have a
    // SalesIntelligence.CustomerRecommendations row — as do all 847 customers — so he is not
    // deletable. See ADR-0009.
    private const int KnownSeededCustomerId = 1;

    private AdventureWorksLtDbContext _dbContext = null!;
    private CustomerWriteRepository _repository = null!;
    private readonly List<int> _createdCustomerIds = new();

    [SetUp]
    public void Setup()
    {
        _dbContext = DbContextTestFactory.Create();
        _repository = new CustomerWriteRepository(_dbContext);
    }

    [TearDown]
    public async Task TearDown()
    {
        foreach (var customerId in _createdCustomerIds)
        {
            try
            {
                await _repository.DeleteAsync(customerId);
            }
            catch
            {
                // Best-effort cleanup: if delete also fails there's nothing further this test
                // teardown can do, and swallowing here must not mask the original test failure.
            }
        }

        _createdCustomerIds.Clear();
        _dbContext.Dispose();
    }

    // SalesLT.Customer.EmailAddress is nvarchar(50) (see AdventureWorksLtDbContext's
    // HasMaxLength(50)) -- keep generated addresses well under that.
    private static string BuildUniqueEmail(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return $"{prefix}-{suffix}@ex.com";
    }

    private static CreateCustomerDto BuildCreateDto(string firstName = "Integration", string lastName = "Test")
    {
        return new CreateCustomerDto
        {
            FirstName = firstName,
            LastName = lastName,
            EmailAddress = BuildUniqueEmail("it")
        };
    }

    // --- CreateAsync -----------------------------------------------------------------------

    [Test]
    public async Task CreateAsync_InsertsCustomer_AndReadBackConfirmsPersistence()
    {
        var createDto = BuildCreateDto();

        var created = await _repository.CreateAsync(createDto);
        _createdCustomerIds.Add(created.CustomerId);

        Assert.That(created.CustomerId, Is.GreaterThan(0));
        Assert.That(created.FirstName, Is.EqualTo(createDto.FirstName));
        Assert.That(created.LastName, Is.EqualTo(createDto.LastName));
        Assert.That(created.EmailAddress, Is.EqualTo(createDto.EmailAddress));

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new CustomerReadRepository(verifyContext);
        var readBack = await readRepository.GetByIdAsync(created.CustomerId);

        Assert.That(readBack, Is.Not.Null);
        Assert.That(readBack!.EmailAddress, Is.EqualTo(createDto.EmailAddress));
    }

    // --- UpdateAsync -----------------------------------------------------------------------

    [Test]
    public async Task UpdateAsync_ExistingCustomer_UpdatesFieldsAndReturnsTrue()
    {
        var created = await _repository.CreateAsync(BuildCreateDto());
        _createdCustomerIds.Add(created.CustomerId);

        var updateDto = new UpdateCustomerDto
        {
            FirstName = "Updated",
            LastName = "Customer",
            EmailAddress = BuildUniqueEmail("it-upd")
        };

        var result = await _repository.UpdateAsync(created.CustomerId, updateDto);

        Assert.That(result, Is.True);

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new CustomerReadRepository(verifyContext);
        var updated = await readRepository.GetByIdAsync(created.CustomerId);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.FirstName, Is.EqualTo("Updated"));
        Assert.That(updated.EmailAddress, Is.EqualTo(updateDto.EmailAddress));
    }

    [Test]
    public async Task UpdateAsync_UnknownCustomer_ReturnsFalse()
    {
        var updateDto = new UpdateCustomerDto
        {
            FirstName = "Ghost",
            LastName = "Customer",
            EmailAddress = BuildUniqueEmail("it-ghost")
        };

        var result = await _repository.UpdateAsync(999999, updateDto);

        Assert.That(result, Is.False);
    }

    // --- DeleteAsync -----------------------------------------------------------------------

    [Test]
    public async Task DeleteAsync_ExistingCustomer_RemovesRowAndReturnsTrue()
    {
        var created = await _repository.CreateAsync(BuildCreateDto());
        _createdCustomerIds.Add(created.CustomerId);

        var result = await _repository.DeleteAsync(created.CustomerId);

        Assert.That(result, Is.True);

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new CustomerReadRepository(verifyContext);
        var afterDelete = await readRepository.GetByIdAsync(created.CustomerId);

        Assert.That(afterDelete, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_UnknownCustomer_ReturnsFalse()
    {
        var result = await _repository.DeleteAsync(999999);

        Assert.That(result, Is.False);
    }

    // The case the rest of this file structurally cannot reach. Every other delete test uses a
    // customer created seconds earlier, which is the only kind with no dependent rows — so they all
    // pass while DELETE was returning 500 for all 847 real customers (see ADR-0009).
    //
    // Every FK in this database is NO_ACTION and every seeded customer is referenced by something
    // (customer 1 has no address, no order and no rewards tier, but does have a
    // SalesIntelligence.CustomerRecommendations row), so this must throw. The API turns it into a
    // 409 via DatabaseConflictExceptionHandler; at this layer it's still the raw EF exception.
    [Test]
    public void DeleteAsync_SeededCustomerWithDependentRows_ThrowsRatherThanDeleting()
    {
        Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _repository.DeleteAsync(KnownSeededCustomerId));
    }

    [Test]
    public async Task DeleteAsync_SeededCustomerWithDependentRows_LeavesTheCustomerIntact()
    {
        try
        {
            await _repository.DeleteAsync(KnownSeededCustomerId);
        }
        catch (DbUpdateException)
        {
            // Expected — asserted in the test above. What matters here is the row surviving.
        }

        using var verifyContext = DbContextTestFactory.Create();
        var readRepository = new CustomerReadRepository(verifyContext);
        var stillThere = await readRepository.GetByIdAsync(KnownSeededCustomerId);

        Assert.That(stillThere, Is.Not.Null);
    }
}
