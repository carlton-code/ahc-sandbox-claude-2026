# ADR-0008: Enforce one rewards tier per customer at the schema level

## Status

Accepted

## Context

`Rewards.CustomerRewardsLevel` holds exactly two columns — `CustomerId` and `RewardsLevelId`,
both `NOT NULL`, both with a valid foreign key (`SalesLT.Customer` and `Rewards.RewardsLevel`
respectively). It had **no primary key, no unique index, and no constraints of any kind**: a bare
heap. Nothing at the schema level prevented a customer from holding two tiers at once, or the
identical row being inserted twice.

The data has never exercised that latitude — 552 rows across 552 distinct customers, zero
duplicates — so the "one tier per customer" invariant held by convention only, enforced by nothing.
Building the `GET /api/v1/customers/{id}/rewards` endpoint forced the question, because the read's
shape depends on the answer: one tier means a single flat DTO, many tiers means an array.

Its two-FK-columns-no-payload shape reads like a classic many-to-many bridge table, which pulls
toward the composite key `(CustomerId, RewardsLevelId)`. Three things argue against that reading:

1. **A rewards tier is mutually exclusive by nature.** Holding Gold and Bronze simultaneously
   isn't a state the business has a meaning for.
2. **Two existing stored procedures already assume one tier per customer.**
   `SalesLT.usp_GetCustomerByID` and `SalesLT.usp_GetCustomerBySearchTerm` (2022) both
   `LEFT JOIN` through this table onto a customer row. A second tier row for one customer would
   silently duplicate that customer in both result sets — a live latent bug, not a hypothetical.
3. **There is no history column.** No effective date, no `ModifiedDate`. If the table were meant
   to record tier changes over time, there'd be no way to tell which of two rows was current.

The realistic alternatives were: enforce the invariant in the schema; enforce it defensively in
the read query (`TOP 1` with an explicit `ORDER BY`) and leave the table alone; or accept
many-to-many and return an array.

## Decision

Add a primary key on **`CustomerId` alone**, via a one-off DDL statement:

```sql
ALTER TABLE Rewards.CustomerRewardsLevel
    ADD CONSTRAINT PK_CustomerRewardsLevel PRIMARY KEY CLUSTERED (CustomerId);
```

`CustomerId` alone, deliberately **not** the composite `(CustomerId, RewardsLevelId)` the bridge-table
shape suggests. The composite only prevents the same pair twice — it would still permit a customer
to hold Gold *and* Silver, which is the exact thing being ruled out. Only a key on `CustomerId`
enforces at most one tier per customer.

## Consequences

- The invariant the code relies on is now enforced where it belongs, so
  `CustomerReadRepository.GetRewardsAsync` can read a single tier without a defensive `TOP 1` and
  without the risk of silently picking an arbitrary row.
- The two existing stored procedures are protected from the duplicate-customer-row bug described
  above. This ADR fixes latent database code, not just new application code.
- The table converts from a heap to a clustered index on `CustomerId`, which is the lookup
  predicate every rewards read uses. A small, free performance win.
- **Assigning tiers is now constrained.** A future `PUT /rewards` must be an upsert
  (update-or-insert) rather than a blind insert — a second insert for the same customer will throw
  a PK violation instead of silently creating a duplicate. That's the intended behavior, but it's
  a real constraint on the write path when it gets built.
- **If this database is re-provisioned from scratch, the constraint is lost** and duplicate tier
  rows become possible again. No migrations tooling owns this schema (see ADR-0002 and
  `.claude/rules/ef-core-conventions.md`), so this is a manual DDL step recorded here for anyone
  reproducing the environment — the same situation as ADR-0007's column drop. Re-run the statement
  above.
- This schema now diverges further from anything reproducible by a stock AdventureWorksLT restore.
  The `Rewards` schema is custom to this database already, so the divergence is in the custom part,
  not the sample part.
- A genuine many-to-many customer↔tier relationship is now foreclosed without dropping the
  constraint. Given tiers are mutually exclusive, that's the point rather than a cost — but it is
  a door closed.
