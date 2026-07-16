using System;
using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.Products.Dtos
{
    /// <summary>
    /// Body for <c>PUT /api/v1/products/{productId}</c>. A PUT replaces the whole record, so the
    /// same fields are required here as on create — see <see cref="CreateProductDto"/> for why the
    /// annotations, and the nullable-with-<c>[Required]</c> value types, are load-bearing rather
    /// than decorative.
    /// </summary>
    public class UpdateProductDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; init; } = string.Empty;

        [Required]
        [StringLength(25)]
        public string ProductNumber { get; init; } = string.Empty;

        [StringLength(15)]
        public string? Color { get; init; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal? StandardCost { get; init; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal? ListPrice { get; init; }

        [StringLength(5)]
        public string? Size { get; init; }

        [Range(0.01, 999999.99)]
        public decimal? Weight { get; init; }

        public int? ProductCategoryId { get; init; }

        public int? ProductModelId { get; init; }

        [Required]
        [Range(typeof(DateTime), "1753-01-01", "9999-12-31", ParseLimitsInInvariantCulture = true)]
        public DateTime? SellStartDate { get; init; }

        [Range(typeof(DateTime), "1753-01-01", "9999-12-31", ParseLimitsInInvariantCulture = true)]
        public DateTime? SellEndDate { get; init; }

        [Range(typeof(DateTime), "1753-01-01", "9999-12-31", ParseLimitsInInvariantCulture = true)]
        public DateTime? DiscontinuedDate { get; init; }
    }
}
