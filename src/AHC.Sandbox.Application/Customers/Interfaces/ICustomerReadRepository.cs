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
        Task<IReadOnlyCollection<CustomerOrderDto>> GetOrdersByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerOrderDto?> GetOrderByIdAsync(int customerId, int orderId, CancellationToken cancellationToken = default);
        Task<CustomerSummaryDto?> GetSummaryAsync(int customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<CustomerOrderDto>> GetRecentOrdersAsync(int customerId, int count = 5, CancellationToken cancellationToken = default);
        Task<CustomerOrderSummaryDto?> GetOrderSummaryAsync(int customerId, CancellationToken cancellationToken = default);
        Task<CustomerRewardsDto?> GetRewardsAsync(int customerId, CancellationToken cancellationToken = default);
    }

}
