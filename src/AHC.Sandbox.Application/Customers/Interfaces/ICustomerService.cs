using AHC.Sandbox.Application.Customers.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AHC.Sandbox.Application.Customers.Interfaces
{

    public interface ICustomerService
    {
        Task<IReadOnlyCollection<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
        Task<CustomerDto?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<CustomerDto>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default);
        Task<bool> UpdateCustomerAsync(int customerId, UpdateCustomerDto customer, CancellationToken cancellationToken = default);
        Task<CustomerDto?> PatchCustomerAsync(int customerId, PatchCustomerDto customer, CancellationToken cancellationToken = default);
        Task<bool> DeleteCustomerAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerOrderSummaryDto?> GetCustomerOrderSummaryAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerRewardsDto?> GetCustomerRewardsAsync(int customerId, CancellationToken cancellationToken = default);
    }
}
