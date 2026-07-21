using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

// Plain EF over mapped tables, like AddressReadRepository — see
// .claude/rules/ef-core-conventions.md. Nothing here needs raw SQL.
public class AddressWriteRepository : IAddressWriteRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public AddressWriteRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerAddressDto> CreateForCustomerAsync(
        int customerId,
        CreateCustomerAddressDto address,
        CancellationToken cancellationToken = default)
    {
        var addressEntity = new AddressEntity
        {
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            StateProvince = address.StateProvince,
            CountryRegion = address.CountryRegion,
            PostalCode = address.PostalCode
        };

        // The link row references the new address through the navigation property rather than by
        // id: AddressID is IDENTITY, so there's no id to copy until SaveChangesAsync runs. EF
        // orders the two inserts and fixes the generated key into the link row's foreign key, so
        // one round trip covers both and no orphan address ever exists in between.
        var linkEntity = new CustomerAddressEntity
        {
            CustomerId = customerId,
            AddressType = address.AddressType,
            Address = addressEntity
        };

        _dbContext.CustomerAddresses.Add(linkEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return AddressMapper.ToCustomerAddressDto(addressEntity, linkEntity.AddressType);
    }

    public async Task<bool> UpdateAsync(
        int addressId,
        UpdateAddressDto address,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.AddressLine1 = address.AddressLine1;
        entity.AddressLine2 = address.AddressLine2;
        entity.City = address.City;
        entity.StateProvince = address.StateProvince;
        entity.CountryRegion = address.CountryRegion;
        entity.PostalCode = address.PostalCode;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    // Deliberately does NOT delete the SalesLT.CustomerAddress link rows first. Every foreign key
    // in this database is NO_ACTION, so removing an address that a customer (or an order's
    // ship-to/bill-to) still points at throws DbUpdateException with SQL error 547, which
    // DatabaseConflictExceptionHandler turns into a 409. That refusal is the intended behavior,
    // matching ADR-0009 for customers: the schema is correctly declining to let an address vanish
    // from underneath the rows that reference it.
    //
    // Consequence worth knowing before "fixing" this: because creating an address through this API
    // always writes a link row, DELETE can never succeed on an address this API can reach. See
    // docs/adr/0013-address-writes-split-across-two-route-prefixes.md.
    public async Task<bool> DeleteAsync(int addressId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Addresses.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
