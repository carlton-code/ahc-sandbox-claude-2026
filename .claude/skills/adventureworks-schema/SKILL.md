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

Already mapped in this codebase: `SalesLT.Customer` (see
`Data/Context/AdventureWorksLtDbContext.cs` for the authoritative Fluent API mapping — that's a
better source of truth than this file for the columns it covers) and read via raw SQL:
`SalesLT.SalesOrderHeader` (see `CustomerReadRepository.cs`).

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

### SalesLT.Address — not used by any code

| Column | Type | Nullable |
|---|---|---|
| AddressID (PK) | int | NO |
| AddressLine1 | nvarchar(60) | NO |
| AddressLine2 | nvarchar(60) | YES |
| City | nvarchar(30) | NO |
| StateProvince | nvarchar(50) | NO |
| CountryRegion | nvarchar(50) | NO |
| PostalCode | nvarchar(15) | NO |
| rowguid | uniqueidentifier | NO |
| ModifiedDate | datetime | NO |

### SalesLT.CustomerAddress — not used by any code

| Column | Type | Nullable |
|---|---|---|
| CustomerID (PK, FK → Customer) | int | NO |
| AddressID (PK, FK → Address) | int | NO |
| AddressType | nvarchar(50) | NO |
| rowguid | uniqueidentifier | NO |
| ModifiedDate | datetime | NO |

Composite PK (`CustomerID`, `AddressID`) — map with `HasKey(e => new { e.CustomerId, e.AddressId })`.

### SalesLT.Product — not used by any code

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

### SalesLT.ProductModel / ProductDescription / ProductModelProductDescription — not yet used

- `ProductModel`: `ProductModelID` (PK), `Name` nvarchar(50) NOT NULL, `CatalogDescription` xml
  (nullable), `rowguid`, `ModifiedDate`.
- `ProductDescription`: `ProductDescriptionID` (PK), `Description` nvarchar(400) NOT NULL,
  `rowguid`, `ModifiedDate`.
- `ProductModelProductDescription`: composite PK (`ProductModelID`, `ProductDescriptionID`,
  `Culture`), `Culture` nchar(6) NOT NULL, `rowguid`, `ModifiedDate`. Many-to-many join between
  the two above, keyed per locale.

### SalesLT.SalesOrderHeader — read via raw SQL, not EF-mapped

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

`TotalDue` is computed in the stock schema as `SubTotal + TaxAmt + Freight` — confirm that still
holds here before relying on it rather than reading the stored value. Only the columns
`CustomerReadRepository.cs` already selects (`SalesOrderID`, `CustomerID`, `SalesOrderNumber`,
`OrderDate`, `ShipDate`, `SubTotal`, `TaxAmt`, `Freight`, `TotalDue`) are currently consumed —
extend that query rather than inventing a new one if you just need another column from the same
table.

### SalesLT.SalesOrderDetail — not yet used anywhere in this codebase

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

Composite PK (`SalesOrderID`, `SalesOrderDetailID`). Needed if a future endpoint exposes order
line items.

### Views (read-only, no PK — don't attempt to EF-map with `HasKey`)

- **vGetAllCategories**: `ParentProductCategoryName` nvarchar(50) NOT NULL,
  `ProductCategoryName` nvarchar(50), `ProductCategoryID` int. Flattened category tree — useful
  instead of hand-rolling the `ProductCategory` self-join if a "categories with parent names"
  endpoint is ever needed.
- **vProductAndDescription**: `ProductID`, `Name`, `ProductModel`, `Culture` nchar(6) NOT NULL,
  `Description` nvarchar(400) NOT NULL. Joins Product → ProductModel →
  ProductModelProductDescription → ProductDescription per culture.
- **vProductModelCatalogDescription**: wide catalog-sheet view off `ProductModel` (marketing copy
  fields — `Summary`, `Manufacturer`, `Warranty*`, `Maintenance*`, `Wheel`, `Saddle`, `Pedal`,
  `BikeFrame`, `Crankset`, `Material`, `Color`, `ProductLine`, `Style`, `RiderExperience`, plus
  `ProductURL`, `rowguid`, `ModifiedDate`) — most fields are nullable free text; only pull the
  ones an endpoint actually needs rather than mapping the whole view.

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

## Rewards schema — no code references these yet

Backs a customer rewards-tier program. Use the exact schema name in EF mappings, e.g.
`entity.ToTable("RewardsLevel", "Rewards")`.

- **RewardsLevel**: `RewardsLevelId` (PK, int), `RewardsLevelName` varchar(50) NOT NULL,
  `DiscountPercent` decimal (nullable).
- **CustomerRewardsLevel**: `CustomerId` (FK → `SalesLT.Customer`), `RewardsLevelId` (FK →
  `Rewards.RewardsLevel`) — join table assigning each customer a rewards tier.

None of the `SalesIntelligence`/`Rewards` tables have a Domain entity, Application DTO,
repository, or controller yet. Building an endpoint over any of them is a brand-new vertical
slice (see `.claude/agents/api-scaffolder.md`), and the resulting `DbContext` will span three
schemas (`SalesLT`, `SalesIntelligence`, `Rewards`) with cross-schema foreign keys into
`SalesLT.Product`/`SalesLT.Customer`.

## dbo schema — housekeeping only, never part of the API surface

`BuildVersion`, `ErrorLog`, and `sysdiagrams` are SQL Server/AdventureWorks installation
artifacts (schema version stamp, a proc-error sink, and SSMS's diagram designer metadata,
respectively) — not business data. Don't map or expose these through the API even if asked for
"every table"; flag it back to the user if a request seems to want that.
