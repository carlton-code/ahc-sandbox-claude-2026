using AHC.Sandbox.Application.Caching;
using AHC.Sandbox.Application.Products.Dtos;
using AHC.Sandbox.Application.Products.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace AHC.Sandbox.Infrastructure.Caching
{
    public sealed class RedisProductCacheRepository : IProductCacheRepository
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        private readonly IConnectionMultiplexer _multiplexer;

        public RedisProductCacheRepository(IConnectionMultiplexer multiplexer)
        {
            _multiplexer = multiplexer;
        }

        public async Task<ProductDto?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            RedisValue value;

            try
            {
                var database = _multiplexer.GetDatabase();
                value = await database.StringGetAsync(BuildKey(productId));
            }
            catch (RedisException ex)
            {
                throw new CacheUnavailableException($"Redis read failed for product {productId}.", ex);
            }

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            // Deliberately outside the try/catch above: a deserialization failure here is an
            // application bug (e.g. a ProductDto shape change), not a cache-unavailable
            // condition, and must propagate rather than be mistaken for a Redis outage.
            return JsonSerializer.Deserialize<ProductDto>((string)value!);
        }

        public async Task SetAsync(int productId, ProductDto product, CancellationToken cancellationToken = default)
        {
            var database = _multiplexer.GetDatabase();
            var json = JsonSerializer.Serialize(product);

            try
            {
                await database.StringSetAsync(BuildKey(productId), json, Ttl);
            }
            catch (RedisException ex)
            {
                throw new CacheUnavailableException($"Redis write failed for product {productId}.", ex);
            }
        }

        public async Task RemoveAsync(int productId, CancellationToken cancellationToken = default)
        {
            var database = _multiplexer.GetDatabase();

            try
            {
                await database.KeyDeleteAsync(BuildKey(productId));
            }
            catch (RedisException ex)
            {
                throw new CacheUnavailableException($"Redis invalidation failed for product {productId}.", ex);
            }
        }

        private static string BuildKey(int productId) => $"product:{productId}";
    }
}
