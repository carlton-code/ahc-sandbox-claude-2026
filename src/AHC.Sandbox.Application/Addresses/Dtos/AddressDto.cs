namespace AHC.Sandbox.Application.Addresses.Dtos
{
    /// <summary>
    /// <para>The address record on its own, as served by <c>GET /api/v1/addresses/{addressId}</c>.</para>
    /// <para>
    /// Deliberately has no <c>AddressType</c>, unlike <see cref="CustomerAddressDto"/>: that field
    /// lives on <c>SalesLT.CustomerAddress</c> and describes a customer↔address link, and these
    /// routes have no customer in them.
    /// </para>
    /// </summary>
    public class AddressDto
    {
        public int AddressId { get; init; }
        public string AddressLine1 { get; init; } = string.Empty;
        public string? AddressLine2 { get; init; }
        public string City { get; init; } = string.Empty;
        public string StateProvince { get; init; } = string.Empty;
        public string CountryRegion { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string SingleLineAddress { get; init; } = string.Empty;
    }
}
