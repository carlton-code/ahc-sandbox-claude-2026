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
- No model-validation attributes (`[Required]`, etc.) are present on any DTO today, so the
  framework won't return `400` for a structurally-valid-but-semantically-wrong payload (e.g. an
  empty `FirstName`) — only malformed JSON triggers a framework-level `400`.

## Customers — `api/v1/customers`

**Status:** fully implemented — `Controllers/CustomersController.cs`.

| Method | Path | Request body | Response body | Status codes |
|---|---|---|---|---|
| GET | `/api/v1/customers` | — | `CustomerDto[]` | `200` |
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
