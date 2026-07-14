using Microsoft.Extensions.Configuration;

namespace AHC.Sandbox.IntegrationTests.Infrastructure;

/// <summary>
/// Loads the checked-in <c>appsettings.json</c> once per test run into an <see cref="IConfiguration"/>,
/// mirroring the shape <c>AHC.Sandbox.Data</c>'s <c>AddData</c> and <c>AHC.Sandbox.Infrastructure</c>'s
/// <c>AddInfrastructure</c> read from in the real app.
/// </summary>
public static class TestConfiguration
{
    private static readonly Lazy<IConfiguration> LazyConfiguration = new(BuildConfiguration);

    public static IConfiguration Configuration => LazyConfiguration.Value;

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
    }
}
