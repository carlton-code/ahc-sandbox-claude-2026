using StackExchange.Redis;

namespace AHC.Sandbox.IntegrationTests.Infrastructure;

/// <summary>
/// Builds a real <see cref="IConnectionMultiplexer"/> against the local Redis container, the same
/// way <c>AHC.Sandbox.Infrastructure</c>'s <c>AddInfrastructure</c> does, plus an overload that
/// points at an unreachable endpoint for cache-unavailable failure-path tests.
/// </summary>
public static class RedisTestFixture
{
    /// <summary>
    /// A real connection to the local <c>local-redis</c> container, using the <c>Redis</c>
    /// section of <c>appsettings.json</c>.
    /// </summary>
    public static IConnectionMultiplexer Create()
    {
        var section = TestConfiguration.Configuration.GetSection("Redis");

        var configuration = section["Configuration"] ?? "localhost:6379";
        var useTls = bool.Parse(section["UseTls"] ?? "false");
        var connectTimeoutMs = int.Parse(section["ConnectTimeoutMs"] ?? "1000");

        return Connect(configuration, useTls, connectTimeoutMs);
    }

    /// <summary>
    /// A connection pointed at an endpoint nothing is listening on, with a short timeout, for
    /// exercising the cache-unavailable path without waiting on the real Redis timeout.
    /// </summary>
    public static IConnectionMultiplexer CreateUnreachable()
    {
        return Connect("localhost:6399", useTls: false, connectTimeoutMs: 300);
    }

    private static IConnectionMultiplexer Connect(string endpoint, bool useTls, int connectTimeoutMs)
    {
        var configurationOptions = new ConfigurationOptions
        {
            EndPoints = { endpoint },
            Ssl = useTls,
            ConnectTimeout = connectTimeoutMs,
            SyncTimeout = connectTimeoutMs,
            AsyncTimeout = connectTimeoutMs,
            AbortOnConnectFail = false
        };

        return ConnectionMultiplexer.Connect(configurationOptions);
    }
}
