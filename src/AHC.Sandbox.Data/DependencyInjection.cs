using AHC.Sandbox.Application.Addresses.Interfaces;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Application.Orders.Interfaces;
using AHC.Sandbox.Application.ProductModels.Interfaces;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AHC.Sandbox.Data
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AdventureWorksLtDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("AdventureWorksLt")));

            services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
            services.AddScoped<ICustomerWriteRepository, CustomerWriteRepository>();
            services.AddScoped<IAddressReadRepository, AddressReadRepository>();
            services.AddScoped<IProductReadRepository, ProductReadRepository>();
            services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
            services.AddScoped<IProductModelReadRepository, ProductModelReadRepository>();
            services.AddScoped<IProductModelWriteRepository, ProductModelWriteRepository>();
            services.AddScoped<IOrderReadRepository, OrderReadRepository>();

            return services;
        }
    }
}
