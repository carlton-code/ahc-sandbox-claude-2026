using AHC.Sandbox.Application.Addresses.Interfaces;
using AHC.Sandbox.Application.Addresses.Services;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Application.Customers.Services;
using AHC.Sandbox.Application.Orders.Interfaces;
using AHC.Sandbox.Application.Orders.Services;
using AHC.Sandbox.Application.Products.Interfaces;
using AHC.Sandbox.Application.Products.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AHC.Sandbox.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderService, OrderService>();
            return services;
        }
    }
}
