namespace AHC.Sandbox.Domain.Entities
{
    // A product's category resolved for reads: the subcategory it belongs to plus its parent's
    // name. ParentName is null when the category is itself a root (no product sits on a root today,
    // but a write could put one there). Populated on the read path only.
    public class ProductCategory
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ParentName { get; init; }
    }
}
