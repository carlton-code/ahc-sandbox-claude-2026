using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Application.Customers.Interfaces
{

    public interface ICustomerReadRepository
    {
        Task<IReadOnlyCollection<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<CustomerSummaryDto?> GetSummaryAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerOrderSummaryDto?> GetOrderSummaryAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerRewardsDto?> GetRewardsAsync(int customerId, CancellationToken cancellationToken = default);
    }

}
