namespace AHC.Sandbox.Application.Products.Dtos
{
    // Nested on ProductDto. Resolves the bare category FK to the subcategory name and its parent.
    // parentName is null for a category that is itself a root.
    public class ProductCategoryDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ParentName { get; init; }
    }
}
