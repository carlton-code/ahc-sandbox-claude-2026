using System;
using System.Collections.Generic;
using System.Text;

namespace AHC.Sandbox.Domain.Entities
{
    public class Product
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ProductNumber { get; init; } = string.Empty;
        public string? Color { get; init; }
        public decimal StandardCost { get; init; }
        public decimal ListPrice { get; init; }
        public string? Size { get; init; }
        public decimal? Weight { get; init; }
        public int? ProductCategoryId { get; init; }
        public int? ProductModelId { get; init; }
        public DateTime SellStartDate { get; init; }
        public DateTime? SellEndDate { get; init; }
        public DateTime? DiscontinuedDate { get; init; }

        // Populated on the read path only, from SalesLT.vProductAndDescription (English culture).
        // Null when the product has no English description (one seeded product) or on the write
        // path, which never touches the view.
        public string? Description { get; init; }

        // Resolved category (subcategory name + parent name), populated on the read path only from
        // SalesLT.ProductCategory. Null when the product has no category, or on the write path.
        // ProductCategoryId above remains the raw FK the write path uses.
        public ProductCategory? Category { get; init; }

        public bool IsDiscontinued => DiscontinuedDate is not null;
    }

}
