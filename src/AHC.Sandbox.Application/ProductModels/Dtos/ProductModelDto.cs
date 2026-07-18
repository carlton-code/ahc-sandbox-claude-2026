namespace AHC.Sandbox.Application.ProductModels.Dtos
{
    public class ProductModelDto
    {
        public int ModelId { get; init; }
        public string Name { get; init; } = string.Empty;

        // English marketing description shared by every product on this model; null when the model
        // has none.
        public string? Description { get; init; }
    }
}
