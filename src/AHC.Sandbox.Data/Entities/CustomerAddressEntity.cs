namespace AHC.Sandbox.Data.Entities;

public class CustomerAddressEntity
{
    public int CustomerId { get; set; }
    public int AddressId { get; set; }
    public string AddressType { get; set; } = string.Empty;

    public AddressEntity Address { get; set; } = null!;
}
