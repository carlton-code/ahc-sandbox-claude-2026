# ADR-0011: Consolidate order reads onto the Orders resource, delete the raw order SQL

## Status

Accepted

Supersedes ADR-0010.

## Context

ADR-0010 mapped the order tables for the new read-only `Order` resource and grandfathered the
four raw-ADO.NET customer-order queries in `CustomerReadRepository`, recording their migration
to LINQ as an explicit follow-up decision. That left the API with two ways to read orders:
`/api/v1/orders` (EF LINQ, `OrderDto`/`OrderWithLinesDto`) and the customer-scoped routes
(`/customers/{id}/orders`, `/customers/{id}/orders/{orderId}`, `/customers/{id}/recent-orders`,
`/customers/{id}/order-summary`) backed by the raw queries and `CustomerOrderDto`.

The API has zero users, so there is no backward-compatibility reason to keep the duplicated
surface, and the mapped tables mean the raw SQL no longer pays for itself. The realistic options
were: migrate the four raw queries to LINQ behind unchanged routes, or consolidate the routes
first and let most of the raw code disappear outright.

## Decision

Consolidate, deleting rather than migrating:

- **Removed routes**: `/customers/{id}/orders` (replaced by an optional `?customerId=` filter on
  `GET /api/v1/orders`), `/customers/{id}/orders/{orderId}` (superseded by
  `GET /api/v1/orders/{orderId}`, which also returns lines), and
  `/customers/{id}/recent-orders` (redundant — the filtered list is already newest-first).
  `CustomerOrderDto` went with them.
- **Kept routes**: `/customers/{id}/order-summary` and `/customers/{id}/summary` — aggregate
  reports *about a customer*, not order-resource reads. `GetOrderSummaryAsync` was rewritten as
  a LINQ `GroupBy` aggregate over the mapped `SalesOrderHeaderEntity`; its customer-existence
  probe and zeroed-summary-for-no-orders behavior are unchanged.
- **Deleted raw code**: the three list/by-id queries and their shared `ExecuteOrderQueryAsync`
  helper. The `Rewards` cross-schema join (`GetRewardsAsync`) is now the **only raw SQL** in the
  solution — it stays raw because the `Rewards` tables are unmapped, exactly the line the
  EF-vs-raw rule draws.

Accepted semantic change: `GET /orders?customerId=<unknown>` returns `200 []`, not the removed
sub-resource's `404` — filter semantics, with no customer-existence probe. The summary routes
keep their `404`-for-unknown-customer behavior.

## Consequences

- One way to read orders; the Customer surface shrinks to customer-things (identity, addresses,
  rewards, aggregates).
- Behavior preservation for the kept routes is proven, not assumed: the five pre-existing
  integration tests for `GetSummaryAsync`/`GetOrderSummaryAsync` (pinning exact seed values for
  customers 29485/1/999999) were left byte-identical and pass against the LINQ rewrite, and the
  live JSON responses were byte-compared before/after.
- A client listing a customer's orders must now distinguish "customer doesn't exist" itself if
  it cares — the filtered list can't. In practice `GET /customers/{id}` answers that.
- Anything citing the deleted methods as the raw-SQL reference now points at `GetRewardsAsync`
  (older ADRs keep their historical mentions).
