---
name: adventureworks-schema
description: Column-level reference for every table and view in this project's database, across all three business schemas — SalesLT (stock AdventureWorksLT), SalesIntelligence (bundles/recommendations), and Rewards (customer rewards tiers) — plus the dbo housekeeping tables to leave alone. Use when writing EF entity mappings or raw SQL so table/column names, nullability, lengths, and schema are never guessed.
---

# AdventureWorksLT schema reference

This database has three business schemas and one housekeeping schema:

- **`SalesLT`** — the stock AdventureWorksLT sample tables/views, with two added columns not in
  the public sample (`Product.CurrentDiscount`, `SalesOrderHeader.TrackingNumber`).
- **`SalesIntelligence`** — custom tables for product bundles and recommendations. Not yet
  consumed by any code in this solution.
- **`Rewards`** — custom tables for a customer rewards-tier program. Not yet consumed by any code
  in this solution.
- **`dbo`** — `BuildVersion`, `ErrorLog`, `sysdiagrams`: SQL Server/AdventureWorks installation
  artifacts, not business data. Never map or expose these through the API.

If this database is ever replaced or migrated, refresh this file by running:

```sql
SELECT t.TABLE_SCHEMA, t.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE,
       c.CHARACTER_MAXIMUM_LENGTH, c.IS_NULLABLE
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION;
```

Already mapped in this codebase: `SalesLT.Customer`, `SalesLT.Address`,
`SalesLT.CustomerAddress`, `SalesLT.Product`, `SalesLT.SalesOrderHeader`,
`SalesLT.SalesOrderDetail`, `SalesLT.ProductModel`, `SalesLT.ProductDescription`,
`SalesLT.ProductModelProductDescription`, and the `SalesLT.vProductAndDescription` view (keyless)
(see `Data/Context/AdventureWorksLtDbContext.cs` for the authoritative Fluent API mapping — that's
a better source of truth than this file for the columns it covers).
Read via raw SQL instead, because they're unmapped: the two `Rewards` tables (see
`CustomerReadRepository.cs`, which also keeps four grandfathered raw reads over the now-mapped
`SalesOrderHeader` — see `docs/adr/0010-map-order-tables-keep-legacy-raw-reads.md`).

## SalesLT schema

### SalesLT.Customer — mapped (`CustomerEntity`)

| Column | Type | Nullable |
|---|---|---|
| CustomerID (PK) | int | NO |
| NameStyle | bit | NO |
| Title | nvarchar(8) | YES |
| FirstName | nvarchar(50) | NO |
| MiddleName | nvarchar(50) | YES |
| LastName | nvarchar(50) | NO |
| Suffix | nvarchar(10) | YES |
| CompanyName | nvarchar(128) | YES |
| SalesPerson | nvarchar(256) | YES |
| EmailAddress | nvarchar(50) | YES |
| Phone | nvarchar(25) | YES |
| rowguid | uniqueidentifier | NO |
| ModifiedDate | datetime | NO |

Only `CustomerID`/`Title`/`FirstName`/`MiddleName`/`LastName`/`CompanyName`/`EmailAddress`/`Phone`
are currently mapped in `CustomerEntity` — `NameStyle`, `Suffix`, `SalesPerson`, `rowguid`,
`ModifiedDate` exist in the table but aren't exposed yet. This table has no password columns —
the stock schema's `PasswordHash`/`PasswordSalt` were dropped (see ADR-0007); don't re-add them
expecting a public-sample match.
`EmailAddress` and `Phone` are nullable in the database, but `CustomerEntity`/`Customer` declare
`EmailAddress` as non-nullable (`string`, not `string?`) — a row with a null email will throw on
materialization. Worth flagging rather than silently "fixing" if noticed while scaffolding nearby
code.

### SalesLT.Address — mapped (`AddressEntity`)

| Column | Type | Nullable |
|---|---|---|
| AddressID (PK, **IDENTITY**) | int | NO |
| AddressLine1 | nvarchar(60) | NO |
| AddressLine2 | nvarchar(60) | YES |
| City | nvarchar(30) | NO |
| StateProvince | nvarchar(50) | NO |
| CountryRegion | nvarchar(50) | NO |
| PostalCode | nvarchar(15) | NO |
| rowguid | uniqueidentifier | NO (default `newid()`) |
| ModifiedDate | datetime | NO (default `getdate()`) |

`StateProvince`/`CountryRegion` are the `Name` **alias type** over `nvarchar(50)`, not `nvarchar`
directly — EF maps them as the underlying type. `AddressLine2` is the only nullable text column.

### SalesLT.CustomerAddress — mapped (`CustomerAddressEntity`)

| Column | Type | Nullable |
|---|---|---|
| CustomerID (PK, FK → Customer) | int | NO |
| AddressID (PK, FK → Address) | int | NO |
| AddressType | nvarchar(50) (`Name` alias type) | NO |
| rowguid | uniqueidentifier | NO (default `newid()`) |
| ModifiedDate | datetime | NO (default `getdate()`) |

Composite PK (`CustomerID`, `AddressID`) — mapped with
`HasKey(e => new { e.CustomerId, e.AddressId })`.

Both tables are read with plain LINQ via `Data/Repositories/AddressReadRepository.cs`, behind
`GET /api/v1/customers/{id}/addresses`. `rowguid`/`ModifiedDate` are unmapped on both — safe,
because both have database defaults (unlike ADR-0007's password columns).

**Gotchas:**

- **440 of 847 customers have no address at all** (only 407 do). An empty list is the normal
  majority case — `200 []`, not a 404.
- `AddressType` is only ever **`Main Office`** (407 rows) or **`Shipping`** (10). The 10 customers
  with two addresses have exactly one of each.
- No address is linked to more than one customer today, but nothing enforces that — don't map it
  as a 1:1.
- 450 `Address` rows vs 417 `CustomerAddress` links: **33 addresses belong to no customer**,
  reachable only through `SalesOrderHeader.ShipToAddressID`/`BillToAddressID`, which also FK onto
  this table. That blocks a future hard `DELETE` of an address.

### SalesLT.Product — mapped (`ProductEntity`)

| Column | Type | Nullable |
|---|---|---|
| ProductID (PK) | int | NO |
| Name | nvarchar(50) | NO |
| ProductNumber | nvarchar(25) | NO |
| Color | nvarchar(15) | YES |
| StandardCost | money | NO |
| ListPrice | money | NO |
| Size | nvarchar(5) | YES |
| Weight | decimal | YES |
| ProductCategoryID (FK → ProductCategory) | int | YES |
| ProductModelID (FK → ProductModel) | int | YES |
| SellStartDate | datetime | NO |
| SellEndDate | datetime | YES |
| DiscontinuedDate | datetime | YES |
| ThumbNailPhoto | varbinary(max) | YES |
| ThumbnailPhotoFileName | nvarchar(50) | YES |
| rowguid | uniqueidentifier | NO |
| ModifiedDate | datetime | NO |
| CurrentDiscount *(not in the public sample schema)* | money | NO |

Skip `ThumbNailPhoto`/`ThumbnailPhotoFileName` in any DTO (binary/large, not useful over the API).

### SalesLT.ProductCategory — not used by any code

| Column | Type | Nullable |
|---|---|---|
| ProductCategoryID (PK) | int | NO |
| ParentProductCategoryID (self-FK) | int | YES |
| Name | nvarchar(50) | NO |
| rowguid | uniqueidentifier | NO |
| ModifiedDate | datetime | NO |

### SalesLT.ProductModel / ProductDescription / ProductModelProductDescription — mapped

All three are mapped (`ProductModelEntity`, `ProductDescriptionEntity`,
`ProductModelProductDescriptionEntity`) and back the `ProductModel` resource
(`api/v1/product-models`) and its description upsert (`PUT …/{id}/description`) — see
`ProductModelReadRepository`/`ProductModelWriteRepository` and
`docs/adr/0012-edit-product-descriptions-at-the-model-level.md`. Reads still use the
`vProductAndDescription` view; these tables are the write surface.

- `ProductModel`: `ProductModelID` (PK), `Name` nvarchar(50) NOT NULL, `CatalogDescription` xml
  (nullable, **unmapped**), `rowguid`, `ModifiedDate`. Only `ProductModelID`/`Name` mapped.
- `ProductDescription`: `ProductDescriptionID` (PK, **IDENTITY** → `ValueGeneratedOnAdd`),
  `Description` nvarchar(400) NOT NULL, `rowguid`, `ModifiedDate`. Only the first two mapped;
  `rowguid`/`ModifiedDate` unmapped (defaults) — the create-description path relies on those
  defaults.
- `ProductModelProductDescription`: composite PK (`ProductModelID`, `ProductDescriptionID`,
  `Culture`), `Culture` nchar(6) NOT NULL (**space-padded** — match `LIKE 'en%'`), `rowguid`,
  `ModifiedDate`. Many-to-many join between the two above, keyed per locale. A description edit is
  scoped safely: no `ProductDescription` row is shared across mappings, so updating one touches
  exactly one model+culture.

### SalesLT.SalesOrderHeader — mapped (`SalesOrderHeaderEntity`)

| Column | Type | Nullable |
|---|---|---|
| SalesOrderID (PK) | int | NO |
| RevisionNumber | tinyint | NO |
| OrderDate | datetime | NO |
| DueDate | datetime | NO |
| ShipDate | datetime | YES |
| Status | tinyint | NO |
| OnlineOrderFlag | bit | NO |
| SalesOrderNumber | nvarchar(25) | NO |
| PurchaseOrderNumber | nvarchar(25) | YES |
| AccountNumber | nvarchar(15) | YES |
| CustomerID (FK) | int | NO |
| ShipToAddressID (FK → Address) | int | YES |
| BillToAddressID (FK → Address) | int | YES |
| ShipMethod | nvarchar(50) | NO |
| CreditCardApprovalCode | varchar(15) | YES |
| SubTotal | money | NO |
| TaxAmt | money | NO |
| Freight | money | NO |
| TotalDue | money | NO |
| Comment | nvarchar(max) | YES |
| rowguid | uniqueidentifier | NO |
| ModifiedDate | datetime | NO |
| TrackingNumber *(not in the public sample schema)* | varchar(18) | NO |

`SalesOrderNumber` and `TotalDue` (`SubTotal + TaxAmt + Freight`) are **database-computed** —
mapped with `ValueGeneratedOnAddOrUpdate()` so EF never writes them. `CreditCardApprovalCode` is
payment data and is **never mapped or exposed**; `RevisionNumber`/`OnlineOrderFlag`/`rowguid`/
`ModifiedDate` are unmapped (NOT NULL with database defaults). `CustomerReadRepository.cs` keeps
four grandfathered raw reads over this table (ADR-0010) — new querying goes through
`SalesOrderHeaderEntity` and LINQ.

### SalesLT.SalesOrderDetail — mapped (`SalesOrderDetailEntity`)

| Column | Type | Nullable |
|---|---|---|
| SalesOrderID (PK, FK → SalesOrderHeader) | int | NO |
| SalesOrderDetailID (PK) | int | NO |
| OrderQty | smallint | NO |
| ProductID (FK → Product) | int | NO |
| UnitPrice | money | NO |
| UnitPriceDiscount | money | NO |
| LineTotal | numeric (computed) | NO |
| rowguid | uniqueidentifier | NO |
| ModifiedDate | datetime | NO |

Composite PK (`SalesOrderID`, `SalesOrderDetailID`). `LineTotal` is **database-computed**
(`UnitPrice * (1 - UnitPriceDiscount) * OrderQty`, `numeric(38,6)`), mapped with
`ValueGeneratedOnAddOrUpdate()`. Exposed as `lines` on `GET /api/v1/orders/{orderId}`.

### Views (read-only)

All three are stock AdventureWorksLT views in `SalesLT`, all keyless (no PK). Row counts below
verified against the live database 2026-07-17. To read one through EF, map it as a keyless entity
(`entity.HasNoKey().ToView("<name>", "SalesLT")`) — **not** `HasKey`; a keyless entity is
query-only, so it can't be tracked or written, which is exactly right for a view. Raw parameterized
SQL is the alternative (as with the `Rewards` tables). Only `vProductAndDescription` is consumed by
code today (see below); the other two are unused.

- **vGetAllCategories** — `ParentProductCategoryName` nvarchar(50) NOT NULL,
  `ProductCategoryName` nvarchar(50), `ProductCategoryID` int. **37 rows.** A recursive CTE that
  flattens the `ProductCategory` self-hierarchy to `(parent name, category name, category id)`.
  Only returns categories that *have* a parent — the four roots (`Bikes`, `Components`, `Clothing`,
  `Accessories`) appear only in the `ParentProductCategoryName` column, never as a row of their
  own. Saves hand-rolling the self-join if a "categories with parent names" read is ever needed.
- **vProductAndDescription** — `ProductID` int NOT NULL, `Name` nvarchar(50) NOT NULL,
  `ProductModel` nvarchar(50) NOT NULL, `Culture` nchar(6) NOT NULL, `Description` nvarchar(400)
  NOT NULL. **1,764 rows.** Joins Product → ProductModel → ProductModelProductDescription →
  ProductDescription, one row **per product per culture**. Six cultures are present (`en`, `fr`,
  `th`, `ar`, `he`, `zh-cht`); `Culture` is `nchar(6)`, so it's space-padded (`'en    '`) — match
  with `LIKE 'en%'` or `RTRIM`, not `= 'en'`. Coverage: **294 of 295 products** have an `en`
  description (one product has none, `ProductID` 907 — a `LEFT JOIN`/outer read, not inner, if you
  need all products). **In use:** mapped keyless as `ProductDescriptionView` and joined onto the
  `Product` reads to fill `ProductDto.description` (English only) — see `ProductReadRepository` and
  the `SalesLT.vProductAndDescription` entry in `docs/database-schema.md`.
- **vProductModelCatalogDescription** — wide catalog-sheet view off `ProductModel` (`ProductModelID`
  int NOT NULL, `Name` NOT NULL, then marketing copy: `Summary`/`Manufacturer` nvarchar(max),
  `Copyright`, `ProductURL`, `Warranty*`, `NoOfYears`, `MaintenanceDescription`, `Wheel`, `Saddle`,
  `Pedal`, `BikeFrame`, `Crankset`, `Picture*`, `ProductPhotoID`, `Material`, `Color`,
  `ProductLine`, `Style`, `RiderExperience`, plus `rowguid`, `ModifiedDate`). **Only 6 rows** — it
  parses the `ProductModel.CatalogDescription` XML, and just six models carry that XML. Most fields
  are nullable free text; pull only the columns an endpoint needs rather than mapping the whole
  view. Low value given the tiny, sparse coverage.

## SalesIntelligence schema — no code references these yet

Backs product bundles and recommendations. Use the exact schema name in EF mappings, e.g.
`entity.ToTable("Bundle", "SalesIntelligence")`.

- **Bundle**: `BundleId` (PK, int), `BundleName` varchar(200) NOT NULL, `StartDate` datetime NOT
  NULL, `EndDate` datetime (nullable — open-ended bundle if null).
- **BundleProduct**: `BundleId` (FK → Bundle), `ProductId` (FK → `SalesLT.Product` — cross-schema
  FK), `ProductPrice` money NOT NULL (the product's price *within this bundle*, distinct from
  `Product.ListPrice`). Shaped like a composite-key join table (`BundleId` + `ProductId`), though
  PK/FK constraints aren't confirmed — check `sys.key_constraints`/`sys.foreign_keys` before
  relying on referential integrity being enforced at the DB level.
- **ProductRecommendations**: `ProductId` (FK → `SalesLT.Product`), `RecommendedProductId`
  (presumably also FK → `SalesLT.Product`) — "customers who viewed/bought this also liked" style
  pairing.
- **CustomerRecommendations**: `CustomerId` (FK → `SalesLT.Customer`), `ProductId` (FK →
  `SalesLT.Product`) — per-customer recommended products.

## Rewards schema — read by `GET /api/v1/customers/{id}/rewards`

Backs a customer rewards-tier program. Verified against the live database 2026-07-14. Neither
table is EF-mapped: they're read via raw ADO.NET in `CustomerReadRepository.GetRewardsAsync`, so
use the fully-qualified `Rewards.<Table>` name in SQL. If you ever do map them, use the exact
schema name, e.g. `entity.ToTable("RewardsLevel", "Rewards")`.

**Rewards.RewardsLevel**

| Column | Type | Null | Notes |
|---|---|---|---|
| `RewardsLevelId` | `int` | NO | PK, clustered, **IDENTITY** |
| `RewardsLevelName` | `varchar(50)` | NO | |
| `DiscountPercent` | `decimal(18,4)` | **YES** | needs a `DBNull` guard on read |

Exactly three rows: **Gold = `0`**, Silver = `1`, Bronze = `2`.

**Rewards.CustomerRewardsLevel**

| Column | Type | Null | Notes |
|---|---|---|---|
| `CustomerId` | `int` | NO | **PK** (`PK_CustomerRewardsLevel`, clustered), FK → `SalesLT.Customer.CustomerID` |
| `RewardsLevelId` | `int` | NO | FK → `Rewards.RewardsLevel.RewardsLevelId` |

Two columns only — no surrogate key, no dates, no `ModifiedDate`. **Not** a many-to-many bridge
despite the shape: the PK is on `CustomerId` alone, so a customer has **at most one** tier. See
`docs/adr/0008-one-rewards-tier-per-customer.md` — that PK was added deliberately and is lost if
this database is re-provisioned.

### Gotchas

- **Gold is `RewardsLevelId` `0`**, which collides with `default(int)`. Use `int?` in any DTO or
  mapping so "no tier" is `null` and can never be confused with Gold. Gold also has **zero**
  customers assigned today.
- **`DiscountPercent` is a rate, not a percentage** — the values are `.0010`/`.0009`/`.0008`,
  i.e. 0.1%/0.09%/0.08%. The column name says otherwise. Don't multiply by 100 assuming the name
  is accurate, and don't "fix" the data assuming the values are wrong.
- **295 of 847 customers have no tier row at all.** A customer with no tier is a normal state, not
  an error — read with a `LEFT JOIN` from `SalesLT.Customer`, never an inner join.

The `SalesIntelligence` tables still have no Domain entity, Application DTO, repository, or
controller. Building an endpoint over any of them is a brand-new vertical slice (see
`.claude/agents/api-scaffolder.md`), with cross-schema foreign keys into
`SalesLT.Product`/`SalesLT.Customer`.

## dbo schema — housekeeping only, never part of the API surface

`BuildVersion`, `ErrorLog`, and `sysdiagrams` are SQL Server/AdventureWorks installation
artifacts (schema version stamp, a proc-error sink, and SSMS's diagram designer metadata,
respectively) — not business data. Don't map or expose these through the API even if asked for
"every table"; flag it back to the user if a request seems to want that.
