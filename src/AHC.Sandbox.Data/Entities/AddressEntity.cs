namespace AHC.Sandbox.Data.Entities;

public class AddressEntity
{
    public int AddressId { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string CountryRegion { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}
