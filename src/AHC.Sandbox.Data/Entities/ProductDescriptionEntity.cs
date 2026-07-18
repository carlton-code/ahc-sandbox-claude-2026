namespace AHC.Sandbox.Data.Entities;

public class ProductDescriptionEntity
{
    // IDENTITY in the database, so EF generates it on insert (the create-description branch of
    // ProductModelWriteRepository relies on this).
    public int ProductDescriptionId { get; set; }
    public string Description { get; set; } = string.Empty;
}
