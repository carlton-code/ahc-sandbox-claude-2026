using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Application.Addresses.Interfaces;
using AHC.Sandbox.Data.Context;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Data.Repositories;

// Plain EF/LINQ rather than raw ADO.NET: both tables are mapped, so EF can express this join
// directly. See .claude/rules/ef-core-conventions.md — the raw queries elsewhere in this layer
// exist only because SalesOrderHeader and the Rewards tables aren't mapped, not because joins
// need raw SQL.
public class AddressReadRepository : IAddressReadRepository
{
    private readonly AdventureWorksLtDbContext _dbContext;

    public AddressReadRepository(AdventureWorksLtDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CustomerAddressDto>> GetByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.CustomerAddresses
            .AsNoTracking()
            .Where(ca => ca.CustomerId == customerId)
            // AddressId breaks ties so the multi-address customers come back in a stable order.
            // Only 10 customers have more than one address, and each has one "Main Office" and one
            // "Shipping", so AddressType alone happens to be unique today — AddressId is what
            // makes that a guarantee rather than a coincidence of the current data.
            .OrderBy(ca => ca.AddressType)
            .ThenBy(ca => ca.AddressId)
            .Select(ca => new AddressRow(ca.Address, ca.AddressType))
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(row => MapCustomerAddress(row.Address, row.AddressType))
            .ToArray();
    }

    public async Task<CustomerAddressDto?> GetByCustomerAndAddressIdAsync(
        int customerId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.CustomerAddresses
            .AsNoTracking()
            .Where(ca => ca.CustomerId == customerId && ca.AddressId == addressId)
            .Select(ca => new AddressRow(ca.Address, ca.AddressType))
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : MapCustomerAddress(row.Address, row.AddressType);
    }

    // SingleLineAddress is computed on the Domain entity, so the projection is materialized first
    // and mapped in memory — the same shape as CustomerReadRepository.GetAllAsync.
    private static CustomerAddressDto MapCustomerAddress(AddressEntity entity, string addressType)
    {
        var address = MapAddress(entity);

        return new CustomerAddressDto
        {
            AddressId = address.AddressId,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            StateProvince = address.StateProvince,
            CountryRegion = address.CountryRegion,
            PostalCode = address.PostalCode,
            SingleLineAddress = address.SingleLineAddress,
            AddressType = addressType
        };
    }

    private static Address MapAddress(AddressEntity entity)
    {
        return new Address
        {
            AddressId = entity.AddressId,
            AddressLine1 = entity.AddressLine1,
            AddressLine2 = entity.AddressLine2,
            City = entity.City,
            StateProvince = entity.StateProvince,
            CountryRegion = entity.CountryRegion,
            PostalCode = entity.PostalCode
        };
    }

    private sealed record AddressRow(AddressEntity Address, string AddressType);
}
