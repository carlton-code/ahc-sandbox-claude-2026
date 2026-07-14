using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Infrastructure.Caching;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using StackExchange.Redis;

namespace AHC.Sandbox.IntegrationTests.Caching;

/// <summary>
/// Exercises <see cref="RedisCustomerCacheRepository"/> against the real local Redis container.
/// Each test uses a distinct customer id and removes its key in <see cref="TearDown"/>.
/// </summary>
public class RedisCustomerCacheRepositoryTests
{
    private IConnectionMultiplexer _multiplexer = null!;
    private ICustomerCacheRepository _repository = null!;
    private readonly List<int> _usedCustomerIds = new();

    [SetUp]
    public void Setup()
    {
        _multiplexer = RedisTestFixture.Create();
        _repository = new RedisCustomerCacheRepository(_multiplexer);
    }

    [TearDown]
    public async Task TearDown()
    {
        var database = _multiplexer.GetDatabase();

        foreach (var customerId in _usedCustomerIds)
        {
            await database.KeyDeleteAsync(BuildKey(customerId));
        }

        _usedCustomerIds.Clear();
        _multiplexer.Dispose();
    }

    private static string BuildKey(int customerId) => $"customer:{customerId}";

    private static CustomerDto BuildCustomerDto(int customerId)
    {
        return new CustomerDto
        {
            CustomerId = customerId,
            FirstName = "Integration",
            LastName = "Test",
            FullName = "Integration Test",
            EmailAddress = $"redis-test-{customerId}@example.com"
        };
    }

    // --- Round trip / miss / invalidation ----------------------------------------------------

    [Test]
    public async Task SetAsync_ThenGetByIdAsync_RoundTripsCustomerDto()
    {
        const int customerId = 900001;
        _usedCustomerIds.Add(customerId);
        var customer = BuildCustomerDto(customerId);

        await _repository.SetAsync(customerId, customer);
        var result = await _repository.GetByIdAsync(customerId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CustomerId, Is.EqualTo(customer.CustomerId));
        Assert.That(result.FullName, Is.EqualTo(customer.FullName));
        Assert.That(result.EmailAddress, Is.EqualTo(customer.EmailAddress));
    }

    [Test]
    public async Task GetByIdAsync_KeyNeverSet_ReturnsNull()
    {
        const int customerId = 900002;
        _usedCustomerIds.Add(customerId);

        var result = await _repository.GetByIdAsync(customerId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RemoveAsync_InvalidatesPreviouslySetKey()
    {
        const int customerId = 900003;
        _usedCustomerIds.Add(customerId);
        await _repository.SetAsync(customerId, BuildCustomerDto(customerId));

        await _repository.RemoveAsync(customerId);
        var result = await _repository.GetByIdAsync(customerId);

        Assert.That(result, Is.Null);
    }

    // --- TTL -----------------------------------------------------------------------------------

    [Test]
    public async Task SetAsync_AppliesFiveMinuteTtl()
    {
        const int customerId = 900004;
        _usedCustomerIds.Add(customerId);

        await _repository.SetAsync(customerId, BuildCustomerDto(customerId));

        var database = _multiplexer.GetDatabase();
        var ttl = await database.KeyTimeToLiveAsync(BuildKey(customerId));

        Assert.That(ttl, Is.Not.Null);
        Assert.That(ttl!.Value, Is.LessThanOrEqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(ttl.Value, Is.GreaterThan(TimeSpan.FromMinutes(4)));
    }

    // --- Cache-unavailable path ------------------------------------------------------------------

    [Test]
    public void GetByIdAsync_CacheUnavailable_ThrowsCacheUnavailableException()
    {
        using var unreachableMultiplexer = RedisTestFixture.CreateUnreachable();
        var repository = new RedisCustomerCacheRepository(unreachableMultiplexer);

        Assert.ThrowsAsync<CacheUnavailableException>(() => repository.GetByIdAsync(900005));
    }

    [Test]
    public void SetAsync_CacheUnavailable_ThrowsCacheUnavailableException()
    {
        using var unreachableMultiplexer = RedisTestFixture.CreateUnreachable();
        var repository = new RedisCustomerCacheRepository(unreachableMultiplexer);

        Assert.ThrowsAsync<CacheUnavailableException>(
            () => repository.SetAsync(900006, BuildCustomerDto(900006)));
    }

    [Test]
    public void RemoveAsync_CacheUnavailable_ThrowsCacheUnavailableException()
    {
        using var unreachableMultiplexer = RedisTestFixture.CreateUnreachable();
        var repository = new RedisCustomerCacheRepository(unreachableMultiplexer);

        Assert.ThrowsAsync<CacheUnavailableException>(() => repository.RemoveAsync(900007));
    }
}
