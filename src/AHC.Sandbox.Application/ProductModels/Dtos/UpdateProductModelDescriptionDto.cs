using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.ProductModels.Dtos
{
    /// <summary>
    /// <para>Body for <c>PUT /api/v1/product-models/{modelId}/description</c>.</para>
    /// <para>
    /// Sets the model's English (<c>en</c>) description. <c>[Required]</c> makes a missing, empty,
    /// or whitespace-only value a 400 before the action runs (the binder converts whitespace to
    /// null first), so no hand-rolled guard is needed. <c>[StringLength(400)]</c> mirrors
    /// <c>SalesLT.ProductDescription.Description</c>'s width — without it an over-long value would
    /// reach SQL Server and surface as a 500 rather than a 400.
    /// </para>
    /// </summary>
    public class UpdateProductModelDescriptionDto
    {
        [Required]
        [StringLength(400)]
        public string? Description { get; init; }
    }
}
