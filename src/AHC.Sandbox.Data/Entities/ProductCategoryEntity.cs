namespace AHC.Sandbox.Data.Entities;

// SalesLT.ProductCategory is a self-referencing two-level tree: a root (ParentProductCategoryId
// null) or a subcategory pointing at its root. Products point at subcategories. ParentProductCategoryId
// is kept as a scalar (no navigation) — the read repository self-joins on it for the parent name.
public class ProductCategoryEntity
{
    public int ProductCategoryId { get; set; }
    public int? ParentProductCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}
