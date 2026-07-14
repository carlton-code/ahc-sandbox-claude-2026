using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Application.Customers.Interfaces
{
    public interface ICustomerWriteRepository
    {
        Task<Customer> CreateAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(int customerId, UpdateCustomerDto customer, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int customerId, CancellationToken cancellationToken = default);
    }
}