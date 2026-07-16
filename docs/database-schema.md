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

Order history queried for a customer's orders/summary endpoints.

- **Not EF-mapped** — read via raw parameterized ADO.NET instead (see
  `docs/adr/0002-ef-core-over-dapper.md` for why raw SQL rather than Dapper).
- **Columns currently selected:** `SalesOrderID`, `CustomerID`, `SalesOrderNumber`, `OrderDate`,
  `ShipDate`, `SubTotal`, `TaxAmt`, `Freight`, `TotalDue`.
- **Known gotcha:** this table has a non-standard column, `TrackingNumber`, that isn't part of the
  public AdventureWorksLT sample schema — it's real in this database, don't "correct" it away as
  a typo.
- **Touched by:** `CustomerReadRepository.GetOrdersByCustomerIdAsync` /
  `GetOrderByIdAsync` / `GetRecentOrdersAsync` / `GetOrderSummaryAsync`.

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

## Not in use

No entity, mapping, query, or controller exists for these — nothing here counts as "in use."

- **`SalesLT.ProductCategory`** — no code references this. `Product.ProductCategoryID` FKs onto
  it, but the id is exposed as-is; nothing reads the category itself. (`SalesLT.Product` is now
  in use — see above.)
- The rest of `SalesLT` — `ProductModel`, `ProductDescription`, `ProductModelProductDescription`,
  `SalesOrderDetail`, and the three catalog views (`vGetAllCategories`, `vProductAndDescription`,
  `vProductModelCatalogDescription`).
- **`SalesIntelligence`** — an entirely unbuilt schema (product bundles, recommendations). See the
  schema skill for the table shapes if that work starts. (`Rewards` is now in use — see above.)
- `dbo` housekeeping tables (`BuildVersion`, `ErrorLog`, `sysdiagrams`) — never relevant to this
  API.

## Keeping this current

Move a table from "not in use" to "actually in use" here in the same pass that wires up its EF
mapping/repository (i.e., whenever `.claude/agents/api-scaffolder.md` builds out a new resource).
Keep entries short — a purpose line, what's mapped vs. not, and any gotcha specific to how *this
codebase* uses the table. Exhaustive column-level detail belongs in
`.claude/skills/adventureworks-schema/SKILL.md`, not duplicated here.
