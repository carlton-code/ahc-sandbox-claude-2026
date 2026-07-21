# AHC.Sandbox API Reference

Hand-maintained reference for the HTTP surface of `AHC.Sandbox.Api`, generated from and kept in
sync with the actual controllers under `src/AHC.Sandbox.Api/Controllers/`. This is the checked-in
artifact of record; the live, auto-generated OpenAPI document (`/openapi/v1.json`, browsable via
Swagger UI when the API runs in Development) is the ground truth used to verify this file hasn't
drifted — see `.claude/agents/docs-writer.md` for how the two are kept aligned.

## Conventions

- Base route: `api/v1/<resource>` (see each controller's `[Route("api/v1/[controller]")]`).
  `Program.cs` sets `LowercaseUrls = true`, so the `[controller]` token renders lowercase and the
  paths in this file match the live OpenAPI document character-for-character. Routing itself is
  case-insensitive either way — `/api/v1/Customers` still resolves.
- All request/response bodies are JSON.
- Every status code below is declared on the action via `[ProducesResponseType]`, so the live
  OpenAPI document carries the same routes, status codes, and response schemas this file
  describes — the two are cross-checkable rather than this file being the only record.
- A `404` returns an RFC 9110 `ProblemDetails` body (produced automatically by `[ApiController]`),
  not an empty response.
- No authentication/authorization is configured yet — every endpoint below is open (see
  `docs/adr/` if an ADR exists for when that changes, or `ReadMe-Api.md` for the current gap).
- Write-request DTOs (`Create*`/`Update*`/`Patch*`) carry model-validation attributes
  (`[Required]`, `[StringLength]`, `[EmailAddress]`, `[Range]`, …), and `search`'s `q`
  **query parameter** is `[Required]` — `[ApiController]` turns a violation into a `400` before
  the action body runs, and the attributes mark the constraints in the OpenAPI document. See each
  resource's "Request validation — `400`" section for the exact rules.
- A `400` returns a `ValidationProblemDetails` body — a `ProblemDetails` plus an `errors` map
  keyed by parameter name.
- **Query parameters** are called out inline in the Path column (`?q=<term>`,
  `?customerId=<id>`) rather than getting their own column — `search`'s required `q` and the
  Orders list's optional `customerId` are the only two today.

## Customers — `api/v1/customers`

**Status:** fully implemented — `Controllers/CustomersController.cs`.

| Method | Path | Request body | Response body | Status codes |
|---|---|---|---|---|
| GET | `/api/v1/customers` | — | `CustomerDto[]` | `200` |
| GET | `/api/v1/customers/search?q=<term>` | — | `CustomerDto[]` | `200`, `400` |
| GET | `/api/v1/customers/{customerId}` | — | `CustomerDto` | `200`, `404` |
| POST | `/api/v1/customers` | `CreateCustomerDto` | `CustomerDto` | `201` (+ `Location` header via `CreatedAtAction`), `400` |
| PUT | `/api/v1/customers/{customerId}` | `UpdateCustomerDto` | — | `204`, `400`, `404` |
| PATCH | `/api/v1/customers/{customerId}` | `PatchCustomerDto` | `CustomerDto` | `200`, `400`, `404` |
| DELETE | `/api/v1/customers/{customerId}` | — | — | `204`, `404`, `409` |
| GET | `/api/v1/customers/{customerId}/summary` | — | `CustomerSummaryDto` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/order-summary` | — | `CustomerOrderSummaryDto` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/rewards` | — | `CustomerRewardsDto` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/addresses` | — | `CustomerAddressDto[]` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/addresses/{addressId}` | — | `CustomerAddressDto` | `200`, `404` |
| POST | `/api/v1/customers/{customerId}/addresses` | `CreateCustomerAddressDto` | `CustomerAddressDto` | `201` (+ `Location` header via `CreatedAtAction`), `400`, `404`, `409` |

The two address `GET`s live on `CustomersController`; the `POST` is on `AddressesController`
despite sharing their prefix. See the Addresses section below for why creating an address needs a
customer in the route while editing one doesn't.

There are no order-list routes under customers anymore: `/customers/{id}/orders`,
`/customers/{id}/orders/{orderId}`, and `/customers/{id}/recent-orders` were removed in favor of
`GET /api/v1/orders?customerId=<id>` and `GET /api/v1/orders/{orderId}` (see the Orders section
and ADR-0011). The two aggregate reports (`/summary`, `/order-summary`) stay here — they
describe the customer, not orders as a resource.

### Request validation — `400`

`POST` and `PUT` require `firstName`, `lastName` and `emailAddress`; all three reject empty and
whitespace-only values, not just missing ones. `emailAddress` must look like an email. Every string
field is length-capped to its real column width (`firstName`/`middleName`/`lastName`/`emailAddress`
50, `companyName` 128). A violation is a `400` with `ValidationProblemDetails`, produced by
`[ApiController]` before the action body runs.

`PATCH` applies the same length and format rules but requires nothing — `null` means "leave this
field alone". A consequence worth knowing: `PATCH` therefore **cannot clear** `middleName` or
`companyName` back to null. Use `PUT` to replace the whole record.

These caps are not belt-and-braces. EF's `HasMaxLength` is a mapping hint, not a client-side check,
so before this validation existed a too-long value reached SQL Server and came back as an unhandled
`500`, while a body of `{}` created a customer with an empty name and email and returned `201`.

### Deleting a customer — `409`

`DELETE /api/v1/customers/{customerId}` returns **`409 Conflict`** when anything still references
the customer — an address, a rewards tier, an order, or a recommendation.

In practice that is **every customer in this database**. All 847 have a
`SalesIntelligence.CustomerRecommendations` row, and every foreign key is `NO_ACTION`, so `204` is
reachable only for a customer created through this API that has nothing attached to it yet. That's
intended: the constraints are what stop a customer evaporating while order history still points at
them. See `docs/adr/0009-customer-delete-refuses-rather-than-cascades.md`.

The `409` body is a generic `ProblemDetails` — the SQL constraint detail goes to the log, not to the
caller.

### Search — `GET /api/v1/customers/search?q=<term>`

`q` is **required**; missing, empty, or whitespace-only returns `400` with a
`ValidationProblemDetails` body. The term is matched as a **substring**, **case-insensitively**
(the database's default collation), against:

- a first name — `?q=Orlando`
- a last name — `?q=Gee`
- both, separated by a space — `?q=Orlando%20Gee`

The term is **never split into first/last parts**, because this data makes that unworkable: nine
last names contain a space (e.g. `Van Houten`), so splitting `Roger Van Houten` on the first space
would search for the surname `Van` and find nothing, while splitting from the right breaks the six
first names that contain a space (e.g. `Janaina Barreiro Gambaro`). The whole term is matched
against the two full-name concatenations (with and without the middle name) instead — a first or
last name is itself a substring of the concatenation, so matching the individual columns as well
would be redundant. See `CustomerReadRepository.SearchByNameAsync`.

`LIKE` metacharacters (`%`, `_`, `[`) in `q` are escaped and matched literally — `?q=%` returns an
empty array, not every customer. No matches is `200` with `[]`, never `404`. Results are ordered by
last name, then first name, then customer id, matching `GET /api/v1/customers` — the id is the
tiebreaker, and it's load-bearing: 812 of 847 customers share a name with someone else, so without
it the order of those rows would be whatever the query plan emitted. There is no result cap or
paging, consistent with that endpoint (847 customers is the ceiling).

### DTO shapes (`Application/Customers/Dtos/`)

- **`CustomerDto`**: `customerId` (int), `firstName`, `middleName?`, `lastName`, `fullName`
  (computed: `firstName [middleName] lastName`), `companyName?`, `emailAddress`.
- **`CreateCustomerDto`**: `firstName`, `middleName?`, `lastName`, `companyName?`, `emailAddress`.
- **`UpdateCustomerDto`**: same shape as `CreateCustomerDto` (full replace — all fields required
  by convention even though not annotated as such).
- **`PatchCustomerDto`**: every field nullable/optional — `firstName?`, `middleName?`, `lastName?`,
  `companyName?`, `emailAddress?`. Unset fields keep the existing value (see
  `CustomerService.PatchCustomerAsync`).
- **`CustomerSummaryDto`**: `customer` (`CustomerDto`), `orderCount`, `totalOrderValue`,
  `mostRecentOrderDate?`.
- **`CustomerOrderSummaryDto`**: `customerId`, `orderCount`, `subTotal`, `taxAmount`,
  `freightAmount`, `totalDue`, `firstOrderDate?`, `mostRecentOrderDate?`.
- **`CustomerRewardsDto`**: `customerId` (int), `rewardsLevelId?` (int), `rewardsLevelName?`,
  `discountPercent?` (decimal). **All three tier fields are null together** when the customer
  exists but has no rewards tier assigned — 295 of 847 customers today. That's a `200`, not a
  `404`; `404` means no such customer. `rewardsLevelId` is nullable rather than defaulting to `0`
  because `0` is a real tier (Gold) — see `docs/database-schema.md`. Note `discountPercent` is a
  rate (`0.0009` = 0.09%), not a percentage, despite the name.
- **`CustomerAddressDto`**: `addressId` (int), `addressLine1`, `addressLine2?`, `city`,
  `stateProvince`, `countryRegion`, `postalCode`, `singleLineAddress`, `addressType`.
  `addressLine2` is the only nullable field. `singleLineAddress` is computed, not stored — the
  non-empty parts joined with `", "`. `addressType` describes the customer↔address link rather
  than the address, and is only ever `Main Office` or `Shipping`.

### Addresses — `GET /api/v1/customers/{customerId}/addresses`

A customer with **no addresses is a `200` with an empty array**, not a `404` — that's the majority
case (440 of 847 customers have none). `404` means no such customer.

Addresses are ordered by `addressType`, then `addressId`. The 10 customers who have two addresses
have one `Main Office` and one `Shipping`.

`GET /api/v1/customers/{customerId}/addresses/{addressId}` is scoped to the customer: a real
`addressId` that belongs to a *different* customer returns `404`, not that customer's address.

## Addresses — `api/v1/addresses`

**Status:** fully implemented — `Controllers/AddressesController.cs`.

This resource **deliberately spans two route prefixes**, which is why its controller declares
absolute route templates per action rather than a class-level `[Route]`. `SalesLT.Address` has no
owner column — the customer link and `addressType` live on `SalesLT.CustomerAddress` — so
*creating* an address needs a customer in the route (otherwise it orphans a row nothing can reach),
while *reading, editing and deleting* one doesn't. See
`docs/adr/0013-address-writes-split-across-two-route-prefixes.md`.

| Method | Path | Request body | Response body | Status codes |
|---|---|---|---|---|
| POST | `/api/v1/customers/{customerId}/addresses` | `CreateCustomerAddressDto` | `CustomerAddressDto` | `201` (+ `Location` header via `CreatedAtAction`), `400`, `404`, `409` |
| GET | `/api/v1/addresses/{addressId}` | — | `AddressDto` | `200`, `404` |
| PUT | `/api/v1/addresses/{addressId}` | `UpdateAddressDto` | — | `204`, `400`, `404` |
| DELETE | `/api/v1/addresses/{addressId}` | — | — | `204`, `404`, `409` |

The two customer-scoped address **reads** stay under Customers (see that section) — they're list
views of one customer's addresses, and they're the only place `addressType` is exposed.

`POST` writes the address and its `SalesLT.CustomerAddress` link row in one transaction, so there
is no way to create an unattached address through this API — and equally no way to attach an
existing address to a second customer. `404` on `POST` means no such customer.

### `DELETE` always returns `409`

Deleting an address does **not** unlink it first. Every foreign key in this database is
`NO_ACTION`, so the delete is refused whenever anything still references the address — a customer
link, or an order's `ShipToAddressID`/`BillToAddressID` — and
`DatabaseConflictExceptionHandler` maps that to `409`, exactly as for
`DELETE /api/v1/customers/{customerId}` (ADR-0009).

Because `POST` always writes a link row, and every seeded address already has one, **no address
this endpoint can reach is unreferenced** — so `409` is the only outcome a caller can actually
provoke. `204` is declared and implemented, but not reachable. That's an accepted trade-off, not a
gap: see ADR-0013.

### Request validation — `400`

`POST` requires `addressLine1`, `city`, `stateProvince`, `countryRegion`, `postalCode` and
`addressType`; `PUT` requires the same minus `addressType`. All reject empty and whitespace-only
values, not just missing ones. `addressLine2` is optional on both. Every field is length-capped to
its real column width (`addressLine1`/`addressLine2` 60, `city` 30, `stateProvince` 50,
`countryRegion` 50, `postalCode` 15, `addressType` 50).

`addressType` is **not** validated against `Main Office`/`Shipping` even though those are its only
values in the data — the column has no check constraint, so restricting it here would be the API
inventing a rule the database doesn't have.

`PUT` replaces the whole record, so omitting `addressLine2` clears it. There is no `PATCH` — an
address is a small record replaced wholesale.

### DTOs

- **`AddressDto`**: `addressId` (int), `addressLine1`, `addressLine2?`, `city`, `stateProvince`,
  `countryRegion`, `postalCode`, `singleLineAddress`. Deliberately **no `addressType`** — that
  describes a customer↔address link, and these routes name no customer. Use the customer-scoped
  reads when the type is needed.
- **`CreateCustomerAddressDto`**: the six address fields (`addressLine2` optional) plus
  `addressType`.
- **`UpdateAddressDto`**: the six address fields only. `addressType` is not editable here — it
  belongs to the link row, not the address.

## Products — `api/v1/products`

**Status:** fully implemented — `Controllers/ProductsController.cs`.

| Method | Path | Request body | Response body | Status codes |
|---|---|---|---|---|
| GET | `/api/v1/products` | — | `ProductDto[]` | `200` |
| GET | `/api/v1/products/{productId}` | — | `ProductDto` | `200`, `404` |
| GET | `/api/v1/products/{productId}/model` | — | `ProductModelDto` | `200`, `404` |
| POST | `/api/v1/products` | `CreateProductDto` | `ProductDto` | `201` (+ `Location` header via `CreatedAtAction`), `400`, `409` |
| PUT | `/api/v1/products/{productId}` | `UpdateProductDto` | — | `204`, `400`, `404`, `409` |
| DELETE | `/api/v1/products/{productId}` | — | — | `204`, `404`, `409` |

The list endpoint returns every product (295 seeded) ordered by `name`, then `productId`, with no
paging — consistent with `GET /api/v1/customers`. There's no PATCH or search endpoint yet: only
what the current use cases need (see `.claude/rules/application-conventions.md` on not
over-scaffolding).

### Request validation — `400`

`POST` and `PUT` require `name`, `productNumber`, `standardCost`, `listPrice` and
`sellStartDate`. Strings are length-capped to their real column widths (`name` 50,
`productNumber` 25, `color` 15, `size` 5). `standardCost` and `listPrice` must be `>= 0` and
`weight` `> 0` — `[Range]` attributes mirroring the table's CHECK constraints, so those fail as
`400`s here rather than `409`s at the database.

The required value-type fields are declared nullable-with-`[Required]` on the DTOs deliberately:
declared non-nullable, a missing `standardCost` would silently bind to `0` (a free product, no
error), and a missing `sellStartDate` would bind to year 0001 — outside SQL Server's `datetime`
range — and come back as a `500`. Nullable-plus-`[Required]` turns absence into a `400`.

Not validated here: `sellEndDate >= sellStartDate` is a cross-field CHECK constraint that
single-field annotations can't express — violating it is a `409` from the database, not a `400`.

### Conflicts — `409`

All three mutations can return `409 Conflict` (a generic `ProblemDetails`, produced by
`DatabaseConflictExceptionHandler` — the SQL constraint detail goes to the log, not the caller):

- **`POST`/`PUT`** — a duplicate `name` or `productNumber` (both unique in `SalesLT.Product`), a
  `productCategoryId`/`productModelId` that doesn't exist, or a `sellEndDate` earlier than
  `sellStartDate`.
- **`DELETE`** — anything still references the product: an order line
  (`SalesLT.SalesOrderDetail`) or the `SalesIntelligence` bundle/recommendation tables. Most
  seeded products are referenced, so `204` is realistic mainly for products created through this
  API.

### DTO shapes (`Application/Products/Dtos/`)

- **`ProductDto`**: `productId` (int), `name`, `productNumber`, `color?`, `standardCost`
  (decimal), `listPrice` (decimal), `size?`, `weight?` (decimal), `productModelId?` (int),
  `sellStartDate`, `sellEndDate?`, `discontinuedDate?`, `isDiscontinued`
  (bool — computed: `discontinuedDate != null`), `description?` (string — the English marketing
  copy from `SalesLT.vProductAndDescription`; `null` for the one seeded product that has none),
  and `category?`.
  - **`category`** is a nested object `{ id (int), name, parentName? }` resolved from
    `SalesLT.ProductCategory` — the subcategory the product sits on plus its parent's name
    (`parentName` is `null` for a category that is itself a root). It **replaces** the old bare
    `productCategoryId` on this read DTO. `null` when the product has no category.
  - Both `description` and `category` are **read-only, derived** — not on
    `CreateProductDto`/`UpdateProductDto`, and a just-created product reads them back `null` until
    re-read.
- **`CreateProductDto`**: `name`, `productNumber`, `color?`, `standardCost`, `listPrice`, `size?`,
  `weight?`, `productCategoryId?`, `productModelId?`, `sellStartDate`, `sellEndDate?`,
  `discontinuedDate?`. `productCategoryId` is here (not the nested `category`): you **assign** a
  category by id on write, and `ProductDto.category` is the read-side resolution of it.
- **`UpdateProductDto`**: same shape as `CreateProductDto` (full replace).

The table's binary thumbnail columns (`ThumbNailPhoto`/`ThumbnailPhotoFileName`) are deliberately
not exposed, and its non-standard `CurrentDiscount` column is not mapped — see
`docs/database-schema.md`.

`GET /products/{productId}/model` returns the `ProductModel` this product resolves to (a
discoverability link to the `product-models` resource below). A `404` means the product doesn't
exist — or, in the rare case, has no model.

## Product Models — `api/v1/product-models`

**Status:** read + description write — `Controllers/ProductModelsController.cs`.

| Method | Path | Request body | Response body | Status codes |
|---|---|---|---|---|
| GET | `/api/v1/product-models/{modelId}` | — | `ProductModelDto` | `200`, `404` |
| PUT | `/api/v1/product-models/{modelId}/description` | `UpdateProductModelDescriptionDto` | — | `204`, `400`, `404` |

A product's English marketing description lives on its **model**, shared by every product variant on
that model (e.g. `ProductModelID` 6, "HL Road Frame", backs 11 variants that all show one
description). Editing it here is deliberate — a model-scoped route so the shared effect is explicit,
rather than a `/products/{id}` write that would silently change siblings. See
`docs/adr/0012-edit-product-descriptions-at-the-model-level.md`.

- **`PUT …/description`** sets the English (`en`) description, **create-or-replace**: it updates the
  model's existing description, or creates one if the model has none yet. `204` on success, `404`
  for an unknown model, `400` for a missing/whitespace or over-400-character `description`
  (`ValidationProblemDetails`). The edit changes the description for **every** product on the model.
- **Cache effect:** because `ProductDto.description` is cached per product, a successful edit evicts
  every affected product from the Redis product cache so their next read reflects the new text
  (best-effort — a cache outage is logged, not surfaced).

The route is spelled out explicitly (`api/v1/product-models`) rather than via the `[controller]`
token, which would render `ProductModels` — this is the first multi-word resource.

### DTO shapes (`Application/ProductModels/Dtos/`)

- **`ProductModelDto`**: `modelId` (int), `name`, `description?` (English; `null` when the model has
  none).
- **`UpdateProductModelDescriptionDto`**: `description` (required, max 400).

## Orders — `api/v1/orders`

**Status:** read-only — `Controllers/OrdersController.cs`. There is deliberately no
POST/PUT/DELETE: order creation is a real business workflow (a header plus its lines, money math,
status transitions, a database-computed order number), so write support waits for a genuine use
case and the Domain rules to go with it.

| Method | Path | Request body | Response body | Status codes |
|---|---|---|---|---|
| GET | `/api/v1/orders?customerId=<id>` | — | `OrderDto[]` | `200`, `400` |
| GET | `/api/v1/orders/{orderId}` | — | `OrderWithLinesDto` | `200`, `404` |

The list returns every order (32 seeded) **newest first** (`orderDate` descending, `orderId` as
the tiebreaker — load-bearing, since every seed order shares the single date 2008-06-01), with
headers only: `lines` is not on the list DTO. The by-id read is the one that carries the lines,
ordered by `orderLineId`.

`customerId` is **optional** and this is the only place to read a customer's orders — it
replaced the old `/customers/{id}/orders` sub-resource (ADR-0011). **Filter semantics apply**:
an unknown customer or one with no orders (815 of 847) is a `200` with `[]`, never a `404` —
unlike the removed sub-resource, nothing probes whether the customer exists. A non-integer
`customerId` is the `400` (`ValidationProblemDetails`, from `[ApiController]` model binding).

### DTO shapes (`Application/Orders/Dtos/`)

- **`OrderDto`**: `orderId` (int), `orderNumber` (the database-computed `SO...` number),
  `customerId` (int), `orderDate`, `dueDate`, `shipDate?`, `status` (byte — the raw
  `SalesOrderHeader.Status` code; every seed order is `5`/Shipped), `purchaseOrderNumber?`,
  `accountNumber?`, `shipToAddressId?` (int), `billToAddressId?` (int), `shipMethod`, `subTotal`
  (decimal), `taxAmount` (decimal), `freightAmount` (decimal), `totalDue` (decimal —
  database-computed `subTotal + taxAmount + freightAmount`), `trackingNumber`, `comment?`
  (null on every seed order), `isShipped` (bool — computed: `shipDate != null`; `true` for every
  seed order).
- **`OrderWithLinesDto`**: same fields plus `lines: OrderLineDto[]`.
- **`OrderLineDto`**: `orderLineId` (int), `productId` (int), `orderQty` (short), `unitPrice`
  (decimal), `unitPriceDiscount` (decimal — a rate, `0.05` = 5% off), `lineTotal` (decimal —
  database-computed `unitPrice * (1 - unitPriceDiscount) * orderQty`).

Naming note: a "line" in this API is a `SalesLT.SalesOrderDetail` row — `orderLineId` is
`SalesOrderDetailID`. `taxAmount`/`freightAmount` are the API names for the `TaxAmt`/`Freight`
columns (matching `CustomerOrderSummaryDto`). The table's `CreditCardApprovalCode` column is
payment data and is never mapped or exposed.
