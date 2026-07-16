namespace AHC.Sandbox.Application.Caching
{
    // Thrown by a cache repository implementation (ICustomerCacheRepository,
    // IProductCacheRepository) only when the cache backend itself is unreachable
    // (connection/timeout) — never for application-level bugs, which should propagate normally
    // rather than being mistaken for a cache outage.
    public sealed class CacheUnavailableException : Exception
    {
        public CacheUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
