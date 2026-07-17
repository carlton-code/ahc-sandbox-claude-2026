# ADR-0010: Map the order tables in EF, keep the legacy raw customer-order reads

## Status

Superseded by ADR-0011 — the follow-up this ADR recorded was taken, and the grandfathered raw
reads were removed (mostly by deleting their routes rather than migrating the queries). The
table-mapping half of this decision stands.

## Context

The read-only `Order` resource needed `SalesLT.SalesOrderHeader` and `SalesLT.SalesOrderDetail`.
Until now those tables were unmapped, and the four customer-order queries in
`CustomerReadRepository` (`GetOrdersByCustomerIdAsync`, `GetOrderByIdAsync`,
`GetRecentOrdersAsync`, `GetOrderSummaryAsync`) read `SalesOrderHeader` via raw parameterized
ADO.NET — raw *only because the table wasn't mapped*, per `ef-core-conventions.md`, not because
LINQ couldn't express them.

Mapping the tables (required for the new resource, and this repo's stated preference over adding
more raw SQL) creates a tension: the raw queries now sit on top of a mapped table, which the
"mapped ⇒ LINQ" rule would otherwise forbid. The realistic options were (a) migrate the four raw
queries to LINQ in the same PR, (b) leave them raw indefinitely, or (c) leave them raw now and
record the migration as an explicit, separate decision.

## Decision

Map both order tables (`SalesOrderHeaderEntity`/`SalesOrderDetailEntity`, with the computed
columns `SalesOrderNumber`/`TotalDue`/`LineTotal` marked `ValueGeneratedOnAddOrUpdate()` and
`CreditCardApprovalCode` never mapped), require **all new order querying to be LINQ**, and
**grandfather the four existing raw customer-order queries** — working, integration-tested code
that this PR deliberately does not touch.

Migrating them to LINQ is a real option now and would leave the `Rewards` tables as the only raw
SQL in the solution, but it's a separate follow-up decision with its own PR and its own
verification against the existing integration tests — not something to smuggle into a feature
PR.

## Consequences

- The codebase intentionally holds both styles against the same table for now: LINQ in
  `OrderReadRepository`, raw ADO.NET in `CustomerReadRepository`. Anyone reading them
  side-by-side should land here rather than concluding the rule is inconsistent.
- The raw queries keep their proven behavior and test coverage; nothing user-facing changed in
  the migration-worthy paths.
- The "raw because unmapped" examples in `ef-core-conventions.md` now reduce to the `Rewards`
  tables; the rule file points here for the order-table exception.
- If the follow-up migration happens, the four queries' integration tests are the safety net —
  they pin ordering and column mapping against the real database and should pass unchanged.
