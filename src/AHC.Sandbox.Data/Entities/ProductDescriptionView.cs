namespace AHC.Sandbox.Data.Entities;

/// <summary>
/// Keyless read model over the <c>SalesLT.vProductAndDescription</c> view (Product → ProductModel
/// → ProductModelProductDescription → ProductDescription). The view has one row per product per
/// culture and no key, so it's mapped with <c>HasNoKey().ToView(...)</c> and is query-only — never
/// tracked or written. Only the columns the read path needs are mapped; <c>Name</c> and
/// <c>ProductModel</c> are left off.
/// </summary>
public class ProductDescriptionView
{
    public int ProductId { get; set; }

    // nchar(6), so values are space-padded ('en    '). Filter with LIKE 'en%' (StartsWith),
    // not equality against a trimmed literal.
    public string Culture { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
