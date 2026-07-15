namespace AHC.Sandbox.Application.Addresses.Dtos
{
    public class CustomerAddressDto
    {
        public int AddressId { get; init; }
        public string AddressLine1 { get; init; } = string.Empty;
        public string? AddressLine2 { get; init; }
        public string City { get; init; } = string.Empty;
        public string StateProvince { get; init; } = string.Empty;
        public string CountryRegion { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string SingleLineAddress { get; init; } = string.Empty;

        // Lives on SalesLT.CustomerAddress rather than SalesLT.Address: it describes the link
        // between a customer and an address, not the address itself. Only ever "Main Office" or
        // "Shipping" in this data.
        public string AddressType { get; init; } = string.Empty;
    }
}
