using AHC.Sandbox.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AHC.Sandbox.IntegrationTests.Infrastructure;

/// <summary>
/// Builds a real <see cref="AdventureWorksLtDbContext"/> against the local SQL Server, the same
/// way <c>AHC.Sandbox.Data</c>'s <c>AddData(IConfiguration)</c> does, minus DI.
/// </summary>
public static class DbContextTestFactory
{
    public static AdventureWorksLtDbContext Create()
    {
        var connectionString = TestConfiguration.Configuration.GetConnectionString("AdventureWorksLt");

        var options = new DbContextOptionsBuilder<AdventureWorksLtDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AdventureWorksLtDbContext(options);
    }
}
