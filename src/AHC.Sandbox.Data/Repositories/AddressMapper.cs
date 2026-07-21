using AHC.Sandbox.Application.Addresses.Dtos;
using AHC.Sandbox.Data.Entities;
using AHC.Sandbox.Domain.Entities;

namespace AHC.Sandbox.Data.Repositories;

// Shared by AddressReadRepository and AddressWriteRepository — both need entity → DTO and the
// mapping is identical, so it lives here rather than being duplicated as a private helper in each.
//
// Everything routes through the Domain entity rather than copying fields straight onto the DTO,
// because SingleLineAddress is computed there. That means the projection has to be materialized
// before mapping (EF can't translate the computed property), which is why the read repository
// calls ToArrayAsync first — the same shape as CustomerReadRepository.GetAllAsync.
internal static class AddressMapper
{
    public static Address ToDomain(AddressEntity entity)
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

    public static AddressDto ToAddressDto(AddressEntity entity)
    {
        var address = ToDomain(entity);

        return new AddressDto
        {
            AddressId = address.AddressId,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            StateProvince = address.StateProvince,
            CountryRegion = address.CountryRegion,
            PostalCode = address.PostalCode,
            SingleLineAddress = address.SingleLineAddress
        };
    }

    public static CustomerAddressDto ToCustomerAddressDto(AddressEntity entity, string addressType)
    {
        var address = ToDomain(entity);

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
}
