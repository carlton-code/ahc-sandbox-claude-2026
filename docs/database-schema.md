# Database Schema Notes

AdventureWorksLT's full schema is large — three business schemas (`SalesLT` stock tables,
`SalesIntelligence`, `Rewards`) plus `dbo` housekeeping — and `.claude/skills/adventureworks-schema/SKILL.md`
already has the exhaustive, verified column-level reference across all of it. This file is
deliberately smaller: **only the tables this codebase actually touches today**, why, and the
gotchas already discovered, so that context doesn't have to be rediscovered from scratch each
time. If you need a column list, a type, or a schema name, go to the skill — this file won't
repeat that detail.

## Tables actually in use today

### `SalesLT.Customer`

The customer record backing the one fully-implemented resource, `Customer`.

- **Mapped by:** `CustomerEntity` + the Fluent API config in
  `Data/Context/AdventureWorksLtDbContext.cs`.
- **Columns currently mapped:** `CustomerID`, `Title`, `FirstName`, `MiddleName`, `LastName`,
  `CompanyName`, `EmailAddress`, `Phone` — a subset of the real table. `NameStyle`, `Suffix`,
  `SalesPerson`, `rowguid`, `ModifiedDate` exist on the table but aren't mapped, and shouldn't be
  unless a real use case needs them.
- **No password columns.** The stock schema's `PasswordHash`/`PasswordSalt` columns were dropped
  from this table — this API has no DB-backed authentication concept (auth, if built, will be
  handled by an external IdP). See ADR-0007.
- **Known gotcha:** `EmailAddress` and `Phone` are nullable in the real database, but
  `CustomerEntity`/`Customer` both declare `EmailAddress` as non-nullable (`string`, not
  `string?`) — a row with a null email will throw on materialization. Not yet fixed; flag it
  again if working nearby rather than assuming it's been handled.
- **Touched by:** `Data/Repositories/CustomerReadRepository.cs`, `CustomerWriteRepository.cs`.

### `SalesLT.SalesOrderHeader`

Order headers, backing the `Order` resource (`api/v1/orders`, read-only) and the customer
orders/summary endpoints.

- **EF-mapped** by `SalesOrderHeaderEntity` + Fluent API config in
  `Data/Context/AdventureWorksLtDbContext.cs`, read with plain LINQ everywhere:
  `Data/Repositories/OrderReadRepository.cs` for the Orders endpoints and
  `CustomerReadRepository.GetOrderSummaryAsync` (a LINQ `GroupBy` aggregate) for the customer
  summary reports. The old raw-ADO.NET customer-order queries were removed with their routes —
  see ADR-0011.
- **Columns currently mapped:** `SalesOrderID`, `SalesOrderNumber`, `CustomerID`, `OrderDate`,
  `DueDate`, `ShipDate`, `Status`, `PurchaseOrderNumber`, `AccountNumber`, `ShipToAddressID`,
  `BillToAddressID`, `ShipMethod`, `SubTotal`, `TaxAmt`, `Freight`, `TotalDue`,
  `TrackingNumber`, `Comment`.
- **Never map `CreditCardApprovalCode`** — payment data that must not reach the API surface.
- **Deliberately unmapped:** `RevisionNumber`/`OnlineOrderFlag`/`rowguid`/`ModifiedDate` — all
  NOT NULL but all with database defaults, the usual precedent.
- **Known gotcha:** `SalesOrderNumber` and `TotalDue` are **database-computed** columns, mapped
  with `ValueGeneratedOnAddOrUpdate()` so EF never tries to write them.
- **Known gotcha:** this table has a non-standard column, `TrackingNumber` (`varchar(18)` NOT
  NULL), that isn't part of the public AdventureWorksLT sample schema — it's real in this
  database, don't "correct" it away as a typo.
- **Known gotcha:** all 32 seed orders share one `OrderDate` (2008-06-01), have `Status` = 5, a
  non-null `ShipDate`, and a null `Comment` — orderings need an id tiebreaker to be
  deterministic, and the "unshipped" branches are only reachable through unit tests.
- **Touched by:** `Data/Repositories/OrderReadRepository.cs`;
  `CustomerReadRepository.GetOrderSummaryAsync` (LINQ aggregate backing
  `/customers/{id}/order-summary` and `/summary`).

### `SalesLT.SalesOrderDetail`

Order lines, exposed as `lines` on `GET /api/v1/orders/{orderId}`.

- **EF-mapped** by `SalesOrderDetailEntity` + Fluent API config in
  `Data/Context/AdventureWorksLtDbContext.cs`, loaded via `Include` from the header (the header
  has a `Details` navigation; there's no back-navigation, per the CustomerAddress precedent).
- **Columns currently mapped:** `SalesOrderID`, `SalesOrderDetailID`, `OrderQty`, `ProductID`,
  `UnitPrice`, `UnitPriceDiscount`, `LineTotal`. `rowguid`/`ModifiedDate` unmapped as usual.
- **Known gotcha:** **composite primary key** (`SalesOrderID`, `SalesOrderDetailID`) — the detail
  id alone is an identity but not the key.
- **Known gotcha:** `LineTotal` is **database-computed** (`UnitPrice * (1 - UnitPriceDiscount) *
  OrderQty`, stored as `numeric(38,6)`), mapped with `ValueGeneratedOnAddOrUpdate()`.
- **Touched by:** `Data/Repositories/OrderReadRepository.cs` (`GetByIdAsync` only — the list
  endpoint reads headers only).

### `Rewards.CustomerRewardsLevel` + `Rewards.RewardsLevel`

The customer rewards-tier assignment, exposed by `GET /api/v1/customers/{id}/rewards`.

- **Not EF-mapped** — read via one raw parameterized `LEFT JOIN` across both tables, in
  `CustomerReadRepository.GetRewardsAsync`.
- **Columns currently selected:** `CustomerRewardsLevel.CustomerId`/`RewardsLevelId`;
  `RewardsLevel.RewardsLevelName`/`DiscountPercent`. That's every column both tables have.
- **One tier per customer, enforced by `PK_CustomerRewardsLevel` on `CustomerId` alone** — added
  by this project, see ADR-0008. It looks like a many-to-many bridge table but isn't. The PK is
  lost if the database is re-provisioned; re-run the `ALTER TABLE` in the ADR.
- **Known gotcha:** `Gold` is `RewardsLevelId` **`0`** — the same as `default(int)`. `CustomerRewardsDto`
  uses `int?` so "no tier" is `null` rather than accidentally reading as Gold. Gold currently has
  no customers assigned at all.
- **Known gotcha:** `DiscountPercent` is `decimal(18,4)` valued `.0010`/`.0009`/`.0008` — those are
  **rates** (0.1%), not percentages, despite the column name. The DTO keeps the database's name
  rather than silently reinterpreting it.
- **Known gotcha:** 295 of 847 customers have no tier row. That's a normal state, so the query
  `LEFT JOIN`s from `SalesLT.Customer` — an inner join would make a third of customers look
  nonexistent. `null` from the repository means "no such customer", never "no tier".
- **Touched by:** `Data/Repositories/CustomerReadRepository.cs` (`GetRewardsAsync`).

### `SalesLT.Address` + `SalesLT.CustomerAddress`

A customer's addresses, exposed by `GET /api/v1/customers/{id}/addresses` and
`GET /api/v1/customers/{id}/addresses/{addressId}`.

- **EF-mapped** by `AddressEntity`/`CustomerAddressEntity` + Fluent API config in
  `Data/Context/AdventureWorksLtDbContext.cs`, read with plain LINQ. Mapped rather than read raw
  precisely because `ef-core-conventions.md` says to prefer mapping a new table over adding
  another raw query — the raw SQL elsewhere in this layer exists only for *unmapped* tables.
- **Columns currently mapped:** `Address.AddressID`/`AddressLine1`/`AddressLine2`/`City`/
  `StateProvince`/`CountryRegion`/`PostalCode`, and `CustomerAddress.CustomerID`/`AddressID`/
  `AddressType`. `rowguid`/`ModifiedDate` on both tables are deliberately unmapped.
- **Safe to leave `rowguid`/`ModifiedDate` unmapped**, unlike ADR-0007's password columns: both are
  `NOT NULL` but both have database defaults (`newid()`/`getdate()`), so their absence can't break
  an insert if writes are ever added.
- **Known gotcha:** `StateProvince`, `CountryRegion` and `AddressType` are the `Name` **alias type**
  in this database, not `nvarchar` directly. EF maps them fine as the underlying `nvarchar(50)`.
- **Known gotcha:** **440 of 847 customers have no address at all** — an empty list is the majority
  state, not an error. The API returns `200 []` for it and reserves `404` for a customer that
  doesn't exist, which is why `AddressService` probes the customer before querying addresses.
- **Known gotcha:** `AddressType` is only ever `Main Office` (407 rows) or `Shipping` (10). The 10
  customers with two addresses have one of each — which is why the read orders by `AddressType`
  then `AddressID` rather than by id alone.
- **`SalesOrderHeader.ShipToAddressID`/`BillToAddressID` also FK onto `Address`.** Irrelevant to
  these read-only endpoints, but it means a future hard `DELETE` of an address can violate a
  constraint.
- **Touched by:** `Data/Repositories/AddressReadRepository.cs`.

### `SalesLT.Product`

The product catalog record backing the `Product` resource (`api/v1/products`).

- **Mapped by:** `ProductEntity` + the Fluent API config in
  `Data/Context/AdventureWorksLtDbContext.cs`.
- **Columns currently mapped:** `ProductID`, `Name`, `ProductNumber`, `Color`, `StandardCost`,
  `ListPrice`, `Size`, `Weight`, `ProductCategoryID`, `ProductModelID`, `SellStartDate`,
  `SellEndDate`, `DiscontinuedDate`. These are the model's first non-string/int mappings, so the
  store types are pinned explicitly (`money`, `decimal(8,2)`, `datetime`) rather than left to
  EF's defaults (`decimal(18,2)`, `datetime2` parameters).
- **Deliberately unmapped:** `ThumbNailPhoto`/`ThumbnailPhotoFileName` (binary payloads don't
  belong on this API), plus `rowguid`/`ModifiedDate`/`CurrentDiscount` — all three NOT NULL but
  all with database defaults (`newid()`/`getdate()`/`0`), so, like Address's unmapped columns,
  their absence can't break an insert.
- **Known gotcha:** `CurrentDiscount` is a non-standard column (not in the public AdventureWorksLT
  sample) — real in this database, don't "correct" it away.
- **Known gotcha:** `Name` and `ProductNumber` are **unique** (`AK_Product_Name`,
  `AK_Product_ProductNumber`) — a duplicate on create/update surfaces as a `409` via
  `DatabaseConflictExceptionHandler`, not a validation `400`.
- **Known gotcha:** `Name` is the `Name` alias type (like Address's `StateProvince`); EF maps it
  fine as the underlying `nvarchar(50)`.
- **CHECK constraints:** `StandardCost >= 0`, `ListPrice >= 0`, `Weight > 0` (all mirrored as
  `[Range]` on the write DTOs → `400` at the API), and
  `SellEndDate >= SellStartDate OR SellEndDate IS NULL` (cross-field, not mirrored — violations
  surface as `409`).
- **`SalesLT.SalesOrderDetail.ProductID` and the `SalesIntelligence` tables FK onto this table**,
  so a hard `DELETE` of a referenced product returns `409`; most seeded products are referenced
  by something.
- **Touched by:** `Data/Repositories/ProductReadRepository.cs`, `ProductWriteRepository.cs`.

### `SalesLT.vProductAndDescription` (view)

The English marketing description enriching `ProductDto.description`, joined onto the `Product`
reads (`GET /api/v1/products` and `/{id}`).

- **Mapped by:** `ProductDescriptionView` (keyless) + the Fluent API config in
  `Data/Context/AdventureWorksLtDbContext.cs`. This is the first **view** mapped in the model, and
  the first keyless entity — `HasNoKey().ToView("vProductAndDescription", "SalesLT")`, so EF treats
  it as query-only (no tracking, no writes). Read with LINQ, not raw SQL: a keyless entity + a
  `LEFT JOIN` expresses it fine, so raw ADO.NET (reserved for the unmapped `Rewards` tables) isn't
  warranted.
- **Columns currently mapped:** `ProductID`, `Culture`, `Description`. `Name`/`ProductModel` are
  left unmapped — the join already has the product from `ProductEntity`.
- **Known gotcha:** the view has **one row per product per culture** (6 cultures, ~1,764 rows).
  `ProductReadRepository` filters to English before the join. `Culture` is `nchar(6)`, so its
  values are **space-padded** (`'en    '`) — filter with `LIKE 'en%'` (`EF.Functions.Like` /
  `StartsWith`), never `= 'en'`.
- **Known gotcha:** **294 of 295 products have an English description; one (`ProductID` 907,
  "Rear Brakes") has none.** The join is a `LEFT JOIN` (`DefaultIfEmpty`) precisely so that product
  still returns, with `description: null` — an inner join would silently drop it.
- **Write path leaves it null:** `ProductMapper.ToDomain`'s `description` parameter defaults to
  null and the write repository never reads the view, so a product read back straight after
  `POST`/`PUT` has `description: null` until the view reflects it.
- **Touched by:** `Data/Repositories/ProductReadRepository.cs` (`GetAllAsync`/`GetByIdAsync`).

## Not in use

No entity, mapping, query, or controller exists for these — nothing here counts as "in use."

- **`SalesLT.ProductCategory`** — no code references this. `Product.ProductCategoryID` FKs onto
  it, but the id is exposed as-is; nothing reads the category itself. (`SalesLT.Product` is now
  in use — see above.)
- The rest of `SalesLT` — `ProductModel`, `ProductDescription`, `ProductModelProductDescription`,
  and two of the three catalog views (`vGetAllCategories`, `vProductModelCatalogDescription` — see
  the note below). (`SalesLT.SalesOrderDetail` and the `vProductAndDescription` view are now in
  use — see above.)
- **`SalesIntelligence`** — an entirely unbuilt schema (product bundles, recommendations). See the
  schema skill for the table shapes if that work starts. (`Rewards` is now in use — see above.)
- `dbo` housekeeping tables (`BuildVersion`, `ErrorLog`, `sysdiagrams`) — never relevant to this
  API.

### The `SalesLT` views, and whether they fit existing routes

Column-level detail and gotchas (padded `Culture`, coverage counts, keyless EF mapping) live in the
schema skill's *Views* section. The short version:

- **`vProductAndDescription` is now in use** — it backs `ProductDto.description` (see its "in use"
  entry above). It was the highest-value of the three: the only source of a human-readable product
  description, with 294 of 295 products covered in English.
- **None of the remaining two simplifies an existing query.** No current repository joins
  `ProductCategory` or `ProductModel`, so there's no hand-rolled join a view could replace. They're
  *additive* — they'd enable new data on a route, not tidy up an existing one.
- **`vGetAllCategories`** only helps if we decide to surface category *names*. `ProductDto` exposes
  `ProductCategoryID` as a bare int; this view (parent name + category name + id, 37 rows) could
  back a `/categories` list or add a category name to `ProductDto`. Purely a new feature, not a fix.
- **`vProductModelCatalogDescription`** — 6 rows, sparse marketing XML. Not worth a route on its
  own.

Adding any of these is a new vertical slice (Application DTO + Data read + Api), so it goes through
Plan Mode per `CLAUDE.md`, not a drive-by edit.

## Keeping this current

Move a table from "not in use" to "actually in use" here in the same pass that wires up its EF
mapping/repository (i.e., whenever `.claude/agents/api-scaffolder.md` builds out a new resource).
Keep entries short — a purpose line, what's mapped vs. not, and any gotcha specific to how *this
codebase* uses the table. Exhaustive column-level detail belongs in
`.claude/skills/adventureworks-schema/SKILL.md`, not duplicated here.
