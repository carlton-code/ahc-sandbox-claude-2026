using AHC.Sandbox.Application.Customers.Dtos;

namespace AHC.Sandbox.Application.Customers.Interfaces
{
    public interface ICustomerCacheRepository
    {
        Task<CustomerDto?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task SetAsync(int customerId, CustomerDto customer, CancellationToken cancellationToken = default);
        Task RemoveAsync(int customerId, CancellationToken cancellationToken = default);
    }
}
