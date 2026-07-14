using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Infrastructure.Caching;
using AHC.Sandbox.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AHC.Sandbox.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

            services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
            {
                var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>().Value;

                var configurationOptions = new ConfigurationOptions
                {
                    EndPoints = { redisOptions.Configuration },
                    Ssl = redisOptions.UseTls,
                    ConnectTimeout = redisOptions.ConnectTimeoutMs,
                    SyncTimeout = redisOptions.ConnectTimeoutMs,
                    AsyncTimeout = redisOptions.ConnectTimeoutMs,
                    AbortOnConnectFail = false
                };

                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            services.AddScoped<ICustomerCacheRepository, RedisCustomerCacheRepository>();

            return services;
        }
    }
}
