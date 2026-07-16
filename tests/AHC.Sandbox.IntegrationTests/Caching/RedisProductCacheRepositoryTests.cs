using AHC.Sandbox.Application.Caching;
using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Infrastructure.Caching;
using AHC.Sandbox.IntegrationTests.Infrastructure;
using StackExchange.Redis;

namespace AHC.Sandbox.IntegrationTests.Caching;

/// <summary>
/// Exercises <see cref="RedisProductCacheRepository"/> against the real local Redis container.
/// Each test uses a distinct product id and removes its key in <see cref="TearDown"/>.
/// </summary>
public class RedisProductCacheRepositoryTests
{
    private IConnectionMultiplexer _multiplexer = null!;
    private IProductCacheRepository _repository = null!;
    private readonly List<int> _usedProductIds = new();

    [SetUp]
    public void Setup()
    {
        _multiplexer = RedisTestFixture.Create();
        _repository = new RedisProductCacheRepository(_multiplexer);
    }

    [TearDown]
    public async Task TearDown()
    {
        var database = _multiplexer.GetDatabase();

        foreach (var productId in _usedProductIds)
        {
            await database.KeyDeleteAsync(BuildKey(productId));
        }

        _usedProductIds.Clear();
        _multiplexer.Dispose();
    }

    private static string BuildKey(int productId) => $"product:{productId}";

    private static ProductDto BuildProductDto(int productId)
    {
        return new ProductDto
        {
            ProductId = productId,
            Name = $"Redis Test Product {productId}",
            ProductNumber = $"RT-{productId}",
            StandardCost = 10.50m,
            ListPrice = 19.99m,
            Weight = 1.25m,
            SellStartDate = new DateTime(2026, 1, 1)
        };
    }

    // --- Round trip / miss / invalidation ----------------------------------------------------

    [Test]
    public async Task SetAsync_ThenGetByIdAsync_RoundTripsProductDto()
    {
        const int productId = 910001;
        _usedProductIds.Add(productId);
        var product = BuildProductDto(productId);

        await _repository.SetAsync(productId, product);
        var result = await _repository.GetByIdAsync(productId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ProductId, Is.EqualTo(product.ProductId));
        Assert.That(result.Name, Is.EqualTo(product.Name));
        Assert.That(result.ProductNumber, Is.EqualTo(product.ProductNumber));

        // The decimal and DateTime fields go through JSON — pin that they survive the
        // serialize/deserialize round trip without drifting.
        Assert.That(result.ListPrice, Is.EqualTo(19.99m));
        Assert.That(result.Weight, Is.EqualTo(1.25m));
        Assert.That(result.SellStartDate, Is.EqualTo(new DateTime(2026, 1, 1)));
    }

    [Test]
    public async Task GetByIdAsync_KeyNeverSet_ReturnsNull()
    {
        const int productId = 910002;
        _usedProductIds.Add(productId);

        var result = await _repository.GetByIdAsync(productId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RemoveAsync_InvalidatesPreviouslySetKey()
    {
        const int productId = 910003;
        _usedProductIds.Add(productId);
        await _repository.SetAsync(productId, BuildProductDto(productId));

        await _repository.RemoveAsync(productId);
        var result = await _repository.GetByIdAsync(productId);

        Assert.That(result, Is.Null);
    }

    // --- TTL -----------------------------------------------------------------------------------

    [Test]
    public async Task SetAsync_AppliesFiveMinuteTtl()
    {
        const int productId = 910004;
        _usedProductIds.Add(productId);

        await _repository.SetAsync(productId, BuildProductDto(productId));

        var database = _multiplexer.GetDatabase();
        var ttl = await database.KeyTimeToLiveAsync(BuildKey(productId));

        Assert.That(ttl, Is.Not.Null);
        Assert.That(ttl!.Value, Is.LessThanOrEqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(ttl.Value, Is.GreaterThan(TimeSpan.FromMinutes(4)));
    }

    // --- Cache-unavailable path ------------------------------------------------------------------

    [Test]
    public void GetByIdAsync_CacheUnavailable_ThrowsCacheUnavailableException()
    {
        using var unreachableMultiplexer = RedisTestFixture.CreateUnreachable();
        var repository = new RedisProductCacheRepository(unreachableMultiplexer);

        Assert.ThrowsAsync<CacheUnavailableException>(() => repository.GetByIdAsync(910005));
    }

    [Test]
    public void SetAsync_CacheUnavailable_ThrowsCacheUnavailableException()
    {
        using var unreachableMultiplexer = RedisTestFixture.CreateUnreachable();
        var repository = new RedisProductCacheRepository(unreachableMultiplexer);

        Assert.ThrowsAsync<CacheUnavailableException>(
            () => repository.SetAsync(910006, BuildProductDto(910006)));
    }

    [Test]
    public void RemoveAsync_CacheUnavailable_ThrowsCacheUnavailableException()
    {
        using var unreachableMultiplexer = RedisTestFixture.CreateUnreachable();
        var repository = new RedisProductCacheRepository(unreachableMultiplexer);

        Assert.ThrowsAsync<CacheUnavailableException>(() => repository.RemoveAsync(910007));
    }
}
