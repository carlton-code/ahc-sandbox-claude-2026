namespace AHC.Sandbox.Domain.Entities
{
    public class ProductModel
    {
        public int ProductModelId { get; init; }
        public string Name { get; init; } = string.Empty;

        // The model's English marketing description, resolved on the read path through
        // ProductModelProductDescription → ProductDescription. Null when the model has no English
        // description. Every product on this model shares this one description.
        public string? Description { get; init; }
    }
}
