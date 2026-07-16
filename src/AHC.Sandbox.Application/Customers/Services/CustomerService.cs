using AHC.Sandbox.Application.Caching;
using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Application.Customers.Services
{

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerReadRepository _customerReadRepository;
        private readonly ICustomerWriteRepository _customerWriteRepository;
        private readonly ICustomerCacheRepository _customerCacheRepository;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerReadRepository customerReadRepository,
            ICustomerWriteRepository customerWriteRepository,
            ICustomerCacheRepository customerCacheRepository,
            ILogger<CustomerService> logger)
        {
            _customerReadRepository = customerReadRepository;
            _customerWriteRepository = customerWriteRepository;
            _customerCacheRepository = customerCacheRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
        {
            var customers = await _customerReadRepository.GetAllAsync(cancellationToken);

            return customers
                .Select(MapCustomer)
                .ToArray();
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var (cacheAvailable, cached) = await TryGetCachedCustomerAsync(customerId, cancellationToken);

            if (cached is not null)
            {
                return cached;
            }

            var customer = await _customerReadRepository.GetByIdAsync(customerId, cancellationToken);

            if (customer is null)
            {
                return null;
            }

            var customerDto = MapCustomer(customer);

            // Skip the cache-populate attempt if the read above already showed Redis is
            // unreachable — trying again milliseconds later within the same request would
            // almost certainly fail too, doubling this request's latency for no benefit.
            if (cacheAvailable)
            {
                await TryCacheCustomerAsync(customerId, customerDto, cancellationToken);
            }

            return customerDto;
        }

        public async Task<IReadOnlyCollection<CustomerDto>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var customers = await _customerReadRepository.SearchByNameAsync(searchTerm, cancellationToken);

            return customers
                .Select(MapCustomer)
                .ToArray();
        }

        public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default)
        {
            var createdCustomer = await _customerWriteRepository.CreateAsync(customer, cancellationToken);

            return MapCustomer(createdCustomer);
        }

        public async Task<bool> UpdateCustomerAsync(int customerId, UpdateCustomerDto customer, CancellationToken cancellationToken = default)
        {
            var updated = await _customerWriteRepository.UpdateAsync(customerId, customer, cancellationToken);

            if (updated)
            {
                await TryInvalidateCacheAsync(customerId, cancellationToken);
            }

            return updated;
        }

        public async Task<CustomerDto?> PatchCustomerAsync(int customerId, PatchCustomerDto customer, CancellationToken cancellationToken = default)
        {
            var existingCustomer = await _customerReadRepository.GetByIdAsync(customerId, cancellationToken);

            if (existingCustomer is null)
            {
                return null;
            }

            var updatedCustomer = new UpdateCustomerDto
            {
                FirstName = customer.FirstName ?? existingCustomer.FirstName,
                MiddleName = customer.MiddleName ?? existingCustomer.MiddleName,
                LastName = customer.LastName ?? existingCustomer.LastName,
                CompanyName = customer.CompanyName ?? existingCustomer.CompanyName,
                EmailAddress = customer.EmailAddress ?? existingCustomer.EmailAddress
            };

            var updated = await _customerWriteRepository.UpdateAsync(customerId, updatedCustomer, cancellationToken);

            if (!updated)
            {
                return null;
            }

            // Invalidate before the re-read below so it's guaranteed to be a genuine
            // cache miss against the just-written data, not a stale value.
            await TryInvalidateCacheAsync(customerId, cancellationToken);

            return await GetCustomerByIdAsync(customerId, cancellationToken);
        }

        public async Task<bool> DeleteCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var deleted = await _customerWriteRepository.DeleteAsync(customerId, cancellationToken);

            if (deleted)
            {
                await TryInvalidateCacheAsync(customerId, cancellationToken);
            }

            return deleted;
        }

        public async Task<IReadOnlyCollection<CustomerOrderDto>?> GetCustomerOrdersAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var customer = await _customerReadRepository.GetByIdAsync(customerId, cancellationToken);

            if (customer is null)
            {
                return null;
            }

            return await _customerReadRepository.GetOrdersByCustomerIdAsync(customerId, cancellationToken);
        }

        public Task<CustomerOrderDto?> GetCustomerOrderByIdAsync(int customerId, int orderId, CancellationToken cancellationToken = default)
        {
            return _customerReadRepository.GetOrderByIdAsync(customerId, orderId, cancellationToken);
        }

        public Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return _customerReadRepository.GetSummaryAsync(customerId, cancellationToken);
        }

        public async Task<IReadOnlyCollection<CustomerOrderDto>?> GetCustomerRecentOrdersAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var customer = await _customerReadRepository.GetByIdAsync(customerId, cancellationToken);

            if (customer is null)
            {
                return null;
            }

            return await _customerReadRepository.GetRecentOrdersAsync(customerId, cancellationToken: cancellationToken);
        }

        public Task<CustomerOrderSummaryDto?> GetCustomerOrderSummaryAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return _customerReadRepository.GetOrderSummaryAsync(customerId, cancellationToken);
        }

        public Task<CustomerRewardsDto?> GetCustomerRewardsAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return _customerReadRepository.GetRewardsAsync(customerId, cancellationToken);
        }

        private async Task<(bool CacheAvailable, CustomerDto? Customer)> TryGetCachedCustomerAsync(int customerId, CancellationToken cancellationToken)
        {
            try
            {
                var customer = await _customerCacheRepository.GetByIdAsync(customerId, cancellationToken);

                return (true, customer);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogWarning(ex, "Redis cache read failed for customer {CustomerId}; falling back to database.", customerId);

                return (false, null);
            }
        }

        private async Task TryCacheCustomerAsync(int customerId, CustomerDto customer, CancellationToken cancellationToken)
        {
            try
            {
                await _customerCacheRepository.SetAsync(customerId, customer, cancellationToken);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogWarning(ex, "Redis cache write failed for customer {CustomerId}; continuing without caching this read.", customerId);
            }
        }

        private async Task TryInvalidateCacheAsync(int customerId, CancellationToken cancellationToken)
        {
            try
            {
                await _customerCacheRepository.RemoveAsync(customerId, cancellationToken);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogWarning(ex, "Redis cache invalidation failed for customer {CustomerId}; a stale cached read may be served until it expires.", customerId);
            }
        }

        private static CustomerDto MapCustomer(Customer customer)
        {
            return new CustomerDto
            {
                CustomerId = customer.CustomerId,
                FirstName = customer.FirstName,
                MiddleName = customer.MiddleName,
                LastName = customer.LastName,
                FullName = customer.FullName,
                CompanyName = customer.CompanyName,
                EmailAddress = customer.EmailAddress
            };
        }
    }

}
