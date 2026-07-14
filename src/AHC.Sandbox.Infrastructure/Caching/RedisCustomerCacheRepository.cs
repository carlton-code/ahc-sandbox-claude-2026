using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace AHC.Sandbox.Infrastructure.Caching
{
    public sealed class RedisCustomerCacheRepository : ICustomerCacheRepository
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        private readonly IConnectionMultiplexer _multiplexer;

        public RedisCustomerCacheRepository(IConnectionMultiplexer multiplexer)
        {
            _multiplexer = multiplexer;
        }

        public async Task<CustomerDto?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            RedisValue value;

            try
            {
                var database = _multiplexer.GetDatabase();
                value = await database.StringGetAsync(BuildKey(customerId));
            }
            catch (RedisException ex)
            {
                throw new CacheUnavailableException($"Redis read failed for customer {customerId}.", ex);
            }

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            // Deliberately outside the try/catch above: a deserialization failure here is an
            // application bug (e.g. a CustomerDto shape change), not a cache-unavailable
            // condition, and must propagate rather than be mistaken for a Redis outage.
            return JsonSerializer.Deserialize<CustomerDto>((string)value!);
        }

        public async Task SetAsync(int customerId, CustomerDto customer, CancellationToken cancellationToken = default)
        {
            var database = _multiplexer.GetDatabase();
            var json = JsonSerializer.Serialize(customer);

            try
            {
                await database.StringSetAsync(BuildKey(customerId), json, Ttl);
            }
            catch (RedisException ex)
            {
                throw new CacheUnavailableException($"Redis write failed for customer {customerId}.", ex);
            }
        }

        public async Task RemoveAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var database = _multiplexer.GetDatabase();

            try
            {
                await database.KeyDeleteAsync(BuildKey(customerId));
            }
            catch (RedisException ex)
            {
                throw new CacheUnavailableException($"Redis invalidation failed for customer {customerId}.", ex);
            }
        }

        private static string BuildKey(int customerId) => $"customer:{customerId}";
    }
}
