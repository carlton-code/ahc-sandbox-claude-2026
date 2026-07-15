# ADR-0009: Customer delete refuses rather than cascades

## Status

Accepted

## Context

`DELETE /api/v1/customers/{id}` had never worked. It returned **500 for all 847 customers** in the
database, and shipped that way in v1.0.0.

Three facts combine to cause it:

1. **Every foreign key in this database is `NO_ACTION`** (verified via `sys.foreign_keys`) — no
   cascade anywhere.
2. **Every customer is referenced by something.** All 847 have a
   `SalesIntelligence.CustomerRecommendations` row — a schema this codebase otherwise never touches
   — and most also have an address (407), a rewards tier (552), or an order (32).
3. **There was no exception-handling middleware**, so the resulting `DbUpdateException` surfaced as
   an unhandled 500 rather than anything a caller could act on.

`CustomerWriteRepository.DeleteAsync` calls `Customers.Remove(entity)`, which therefore always
violates a foreign key for a real customer.

The bug survived because of a blind spot in the tests, not an absence of them:
`CustomerWriteRepositoryTests.DeleteAsync_ExistingCustomer_RemovesRowAndReturnsTrue` creates a
customer and immediately deletes it. A freshly-created customer is the *only* kind with no dependent
rows, so the suite proved the one case that works and never touched the 847 that don't.

The realistic options were: add `ON DELETE CASCADE` to the foreign keys so the delete succeeds; add
a soft-delete/`IsActive` column so "delete" becomes an update; return a correct error; or remove the
endpoint.

## Decision

**Return `409 Conflict` when anything still references the customer.** `204` remains reachable only
for a customer created through this API that has nothing attached yet.

The translation happens in `Api/Infrastructure/DatabaseConflictExceptionHandler.cs`, which maps a
`DbUpdateException` carrying SQL error `547`/`2627`/`2601` to a `409` with a generic
`ProblemDetails` body. `CustomerWriteRepository` is unchanged.

Matching on the SQL error number rather than probing for dependents first is deliberate. A probe
would have to enumerate every foreign key pointing at `SalesLT.Customer` — including
`SalesIntelligence`, which this codebase has no other reason to know exists — and would still race
with a concurrent insert landing between the probe and the delete. The database already knows the
answer; this asks it once and translates the reply.

**`ON DELETE CASCADE` was rejected outright.** It's the only change that would make hard delete
succeed broadly, and it would do so by deleting the customer's orders, addresses and rewards tier.
Those `NO_ACTION` foreign keys are not an oversight in Microsoft's schema — they are the schema
refusing to let a customer evaporate while order history still references them. Making the endpoint
"work" would mean destroying the exact data the constraint exists to protect.

**A soft-delete column was also rejected**, for now. It would be the first temporal concept in this
database — the only comparable column in all of `SalesLT` is `Product.SellEndDate` — and ADR-0008
leaned on "there is no history column" as evidence for its own design. It would also be a third
piece of manual DDL, lost by a re-provision, compounding the problem ADR-0007 and ADR-0008 both
document. If a real erasure requirement appears (GDPR-style), that's the point to revisit it, with
its own ADR.

## Consequences

- **`DELETE` now tells the truth.** `409` for the 847 seeded customers, `204` for a customer created
  through this API with nothing attached, `404` for an unknown id. The endpoint is honest rather
  than working, which is the most it can be against this data.
- **The endpoint is a refusal in practice.** Every customer that exists today is reference data with
  dependents. That's the correct answer, but it means `DELETE` is close to decorative — worth
  knowing before building anything that depends on it.
- **The handler is generic, not customer-specific.** Any repository whose write violates a
  constraint now yields a `409` rather than a 500 — including the customer-address writes planned
  next, and the `PUT /rewards` upsert ADR-0008 anticipates (a blind second insert there would hit
  `2627`, now a `409`).
- **The `ProblemDetails` body is deliberately generic.** SQL constraint messages name tables,
  columns and constraints; those go to the log, not to the caller.
- **A test now covers the real case.** `CustomerWriteRepositoryTests` asserts that deleting a
  *seeded* customer throws — the branch the previous suite structurally could not reach.
- **Customer erasure is now unsupported, explicitly.** If someone genuinely needs a customer gone,
  there is no API path and this ADR is the record of why.
