# AHC.Sandbox API Reference

Hand-maintained reference for the HTTP surface of `AHC.Sandbox.Api`, generated from and kept in
sync with the actual controllers under `src/AHC.Sandbox.Api/Controllers/`. This is the checked-in
artifact of record; the live, auto-generated OpenAPI document (`/openapi/v1.json`, browsable via
Swagger UI when the API runs in Development) is the ground truth used to verify this file hasn't
drifted — see `.claude/agents/docs-writer.md` for how the two are kept aligned.

## Conventions

- Base route: `api/v1/<resource>` (see each controller's `[Route("api/v1/[controller]")]`).
- All request/response bodies are JSON.
- Every status code below is declared on the action via `[ProducesResponseType]`, so the live
  OpenAPI document carries the same routes, status codes, and response schemas this file
  describes — the two are cross-checkable rather than this file being the only record.
- A `404` returns an RFC 9110 `ProblemDetails` body (produced automatically by `[ApiController]`),
  not an empty response.
- No authentication/authorization is configured yet — every endpoint below is open (see
  `docs/adr/` if an ADR exists for when that changes, or `ReadMe-Api.md` for the current gap).
- No model-validation attributes (`[Required]`, etc.) are present on any **DTO** today, so the
  framework won't return `400` for a structurally-valid-but-semantically-wrong payload (e.g. an
  empty `FirstName`) — only malformed JSON triggers a framework-level `400`. The sole exception is
  `search`'s `q` **query parameter**, which is `[Required]`; that both marks it required in the
  OpenAPI document and lets the framework return the `400` itself.
- A `400` returns a `ValidationProblemDetails` body — a `ProblemDetails` plus an `errors` map
  keyed by parameter name.
- **Query parameters** are called out inline in the Path column (`?q=<term>`) rather than getting
  their own column — `search` is the only endpoint taking one today.

## Customers — `api/v1/customers`

**Status:** fully implemented — `Controllers/CustomersController.cs`.

| Method | Path | Request body | Response body | Status codes |
|---|---|---|---|---|
| GET | `/api/v1/customers` | — | `CustomerDto[]` | `200` |
| GET | `/api/v1/customers/search?q=<term>` | — | `CustomerDto[]` | `200`, `400` |
| GET | `/api/v1/customers/{customerId}` | — | `CustomerDto` | `200`, `404` |
| POST | `/api/v1/customers` | `CreateCustomerDto` | `CustomerDto` | `201` (+ `Location` header via `CreatedAtAction`) |
| PUT | `/api/v1/customers/{customerId}` | `UpdateCustomerDto` | — | `204`, `404` |
| PATCH | `/api/v1/customers/{customerId}` | `PatchCustomerDto` | `CustomerDto` | `200`, `404` |
| DELETE | `/api/v1/customers/{customerId}` | — | — | `204`, `404` |
| GET | `/api/v1/customers/{customerId}/orders` | — | `CustomerOrderDto[]` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/orders/{orderId}` | — | `CustomerOrderDto` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/summary` | — | `CustomerSummaryDto` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/recent-orders` | — | `CustomerOrderDto[]` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/order-summary` | — | `CustomerOrderSummaryDto` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/rewards` | — | `CustomerRewardsDto` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/addresses` | — | `CustomerAddressDto[]` | `200`, `404` |
| GET | `/api/v1/customers/{customerId}/addresses/{addressId}` | — | `CustomerAddressDto` | `200`, `404` |

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
- **`CustomerOrderDto`**: `orderId`, `customerId`, `orderNumber`, `orderDate`, `shipDate?`,
  `subTotal`, `taxAmount`, `freightAmount`, `totalDue` (all money fields `decimal`).
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
