using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Application.Products.Dtos
{
    public class ProductDto
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ProductNumber { get; init; } = string.Empty;
        public string? Color { get; init; }
        public decimal StandardCost { get; init; }
        public decimal ListPrice { get; init; }
        public string? Size { get; init; }
        public decimal? Weight { get; init; }
        public int? ProductModelId { get; init; }
        public DateTime SellStartDate { get; init; }
        public DateTime? SellEndDate { get; init; }
        public DateTime? DiscontinuedDate { get; init; }
        public bool IsDiscontinued { get; init; }

        // English marketing description from SalesLT.vProductAndDescription; null when the product
        // has none.
        public string? Description { get; init; }

        // Resolved category (replaces the bare productCategoryId that used to be here). Null when
        // the product has no category. Create/Update DTOs still take productCategoryId — you assign
        // a category by id, this is the read-side resolution of it.
        public ProductCategoryDto? Category { get; init; }
    }
}
