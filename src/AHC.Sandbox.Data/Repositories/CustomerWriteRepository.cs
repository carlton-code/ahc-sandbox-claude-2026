using AHC.Sandbox.Application.Customers.Dtos;
using AHC.Sandbox.Application.Customers.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

public class CustomerWriteRepository : ICustomerWriteRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public CustomerWriteRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Customer> CreateAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default)
    {
        var entity = new CustomerEntity
        {
            FirstName = customer.FirstName,
            MiddleName = customer.MiddleName,
            LastName = customer.LastName,
            CompanyName = customer.CompanyName,
            EmailAddress = customer.EmailAddress
        };

        _dbContext.Customers.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCustomer(entity);
    }

    public async Task<bool> UpdateAsync(int customerId, UpdateCustomerDto customer, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.FirstName = customer.FirstName;
        entity.MiddleName = customer.MiddleName;
        entity.LastName = customer.LastName;
        entity.CompanyName = customer.CompanyName;
        entity.EmailAddress = customer.EmailAddress;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Customers.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static Customer MapCustomer(CustomerEntity entity)
    {
        return new Customer
        {
            CustomerId = entity.CustomerId,
            FirstName = entity.FirstName,
            MiddleName = entity.MiddleName,
            LastName = entity.LastName,
            CompanyName = entity.CompanyName,
            EmailAddress = entity.EmailAddress
        };
    }
}
