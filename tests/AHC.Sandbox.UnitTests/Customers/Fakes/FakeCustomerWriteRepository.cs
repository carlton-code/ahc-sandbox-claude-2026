using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.UnitTests.Customers.Fakes
{
    // Minimal hand-written in-memory fake for ICustomerWriteRepository. Records the last request
    // passed to each method so tests can assert on exactly what CustomerService sent, and exposes
    // settable results so tests can drive both the success and failure paths.
    public class FakeCustomerWriteRepository : ICustomerWriteRepository
    {
        public Customer CustomerToReturnOnCreate { get; set; } = new();
        public bool UpdateResult { get; set; } = true;
        public bool DeleteResult { get; set; } = true;

        public CreateCustomerDto? LastCreateRequest { get; private set; }
        public (int CustomerId, UpdateCustomerDto Dto)? LastUpdateRequest { get; private set; }
        public int? LastDeleteRequest { get; private set; }

        public Task<Customer> CreateAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default)
        {
            LastCreateRequest = customer;
            return Task.FromResult(CustomerToReturnOnCreate);
        }

        public Task<bool> UpdateAsync(int customerId, UpdateCustomerDto customer, CancellationToken cancellationToken = default)
        {
            LastUpdateRequest = (customerId, customer);
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteAsync(int customerId, CancellationToken cancellationToken = default)
        {
            LastDeleteRequest = customerId;
            return Task.FromResult(DeleteResult);
        }
    }
}
