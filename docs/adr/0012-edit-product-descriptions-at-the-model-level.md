# ADR-0012: Edit product descriptions at the model level, not per product

## Status

Accepted

## Context

`ProductDto.description` (added as a read-only, derived field) surfaces a product's English
marketing copy. The natural next request was to make it editable. But the description isn't stored
on `SalesLT.Product`: it lives at the end of a chain — `Product.ProductModelID → ProductModel →
ProductModelProductDescription` (keyed per culture) `→ ProductDescription.Description`. The text is
therefore an attribute of the **model**, shared by every product variant on that model.

The data makes this concrete: **213 of 295 products (72%) share their model with at least one other
product** — e.g. `ProductModelID` 6 ("HL Road Frame") backs 11 size/colour variants, all resolving
to one description row. No `ProductDescription` row is referenced by more than one model-culture
mapping, so a description belongs to exactly one model.

Two realistic ways to expose editing:

1. **A product-scoped write** (`PUT /products/{id}/description`) that updates the shared
   `ProductDescription` row. Simple, but for 72% of products it silently rewrites sibling products'
   descriptions — a surprising side effect for a `/products/{id}` route.
2. **A per-product override** — a new app-owned table holding a product-specific description that
   the read prefers over the catalog text. Gives true per-product editing, but adds a table
   (manually provisioned, since this database has no EF migrations — see the `Rewards` precedent in
   ADR-0008), a coalescing read path, and a second source of truth for a field that is, in the
   source schema, genuinely a model attribute.

## Decision

Expose description editing on a **`ProductModel` resource** — `GET /api/v1/product-models/{id}` and
`PUT /api/v1/product-models/{id}/description` (English only) — rather than on the product. The
write is an upsert against the real `ProductModel`/`ProductModelProductDescription`/
`ProductDescription` tables (now EF-mapped), creating the English description row if the model has
none. `ProductDto.description` stays a read-only derived field, and `GET /products/{id}/model`
links a product to its model for discoverability.

The per-product override table (option 2) was rejected: nothing established a real need for
product-specific descriptions, and it would fork a field the schema models at the model level.

## Consequences

- **The route tells the truth about scope.** Editing a model's description changes every product on
  that model, and the URL says `product-models/{id}`, so that's expected rather than surprising. The
  trade-off is that per-product distinct descriptions are *not* supported; if that need ever appears,
  it's the override table, and this ADR is where the rejected alternative is recorded.
- **A cross-resource cache invalidation now exists.** `ProductDto.description` is cached per product
  in Redis, so a model-description edit makes the cached reads of every product on that model stale.
  `ProductModelService` evicts each affected product from `IProductCacheRepository` after the write
  (best-effort, like the other cache paths). This is the first write that invalidates another
  resource's cache — new code touching either side should keep that link in mind.
- **Three more tables are mapped** (`ProductModel`, `ProductDescription`,
  `ProductModelProductDescription`), read/written with EF + LINQ rather than raw SQL, consistent with
  the "prefer mapping over raw ADO.NET" rule. The keyless `vProductAndDescription` view still backs
  the product read; the mapped tables back the model resource and its write.
- **The upsert can create catalog rows.** Editing the description of a model that had none inserts a
  `ProductDescription` and a `ProductModelProductDescription` link (relying on those tables' database
  defaults for `rowguid`/`ModifiedDate`). That's a genuine write into the stock catalog tables, not
  an app-owned side table.
