using System;
using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Application.Products.Dtos
{
    /// <summary>
    /// <para>Body for <c>POST /api/v1/products</c>.</para>
    /// <para>
    /// String lengths mirror the column widths mapped in <c>AdventureWorksLtDbContext</c>, for the
    /// same reason as <see cref="Customers.Dtos.CreateCustomerDto"/> — without them an over-long
    /// value reaches SQL Server and comes back as a 500 instead of a 400.
    /// </para>
    /// <para>
    /// The required value-type fields (<c>StandardCost</c>, <c>ListPrice</c>,
    /// <c>SellStartDate</c>) are declared nullable with <c>[Required]</c> deliberately. Declared
    /// non-nullable, a missing field would silently bind to its default: <c>0</c> for the prices
    /// (a free product, no error), and year-0001 for the date, which is outside SQL Server's
    /// <c>datetime</c> range (min 1753) and fails as a 500. Nullable-plus-<c>[Required]</c> turns
    /// absence into a 400 instead.
    /// </para>
    /// <para>
    /// <c>[Range]</c> mirrors the table's CHECK constraints (<c>StandardCost &gt;= 0</c>,
    /// <c>ListPrice &gt;= 0</c>, <c>Weight &gt; 0</c>) so violations fail as 400s here rather
    /// than 409s at the database. The date fields carry a <c>[Range]</c> floor of 1753-01-01 —
    /// SQL Server's <c>datetime</c> minimum — because an explicitly out-of-range date passes
    /// <c>[Required]</c> and would otherwise fail as a <c>SqlDateTime</c> overflow 500 (a
    /// <c>SqlTypeException</c>, which <c>DatabaseConflictExceptionHandler</c> rightly declines).
    /// The cross-field CHECK (<c>SellEndDate &gt;= SellStartDate</c>) has no single-field
    /// annotation, so that one still surfaces as a 409.
    /// </para>
    /// </summary>
    public class CreateProductDto
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
