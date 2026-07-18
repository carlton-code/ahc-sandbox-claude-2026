namespace AHC.Sandbox.Data.Entities;

// Join between ProductModel and ProductDescription, keyed per culture. Composite PK
// (ProductModelID, ProductDescriptionID, Culture).
public class ProductModelProductDescriptionEntity
{
    public int ProductModelId { get; set; }
    public int ProductDescriptionId { get; set; }
    public string Culture { get; set; } = string.Empty;
}
