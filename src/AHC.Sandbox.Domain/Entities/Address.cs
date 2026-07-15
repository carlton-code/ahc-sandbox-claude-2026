namespace AHC.Sandbox.Domain.Entities
{
    public class Address
    {
        public int AddressId { get; init; }
        public string AddressLine1 { get; init; } = string.Empty;
        public string? AddressLine2 { get; init; }
        public string City { get; init; } = string.Empty;
        public string StateProvince { get; init; } = string.Empty;
        public string CountryRegion { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;

        public string SingleLineAddress =>
            string.Join(
                ", ",
                new[] { AddressLine1, AddressLine2, City, StateProvince, PostalCode, CountryRegion }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
