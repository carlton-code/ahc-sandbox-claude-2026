# ADR-0007: Drop `PasswordHash`/`PasswordSalt` from `SalesLT.Customer`

## Status

Accepted

## Context

`SalesLT.Customer` inherited `PasswordHash` (`varchar(128)`) and `PasswordSalt` (`varchar(10)`)
from Microsoft's stock AdventureWorksLT sample schema. Both are `NOT NULL` with no default, and
neither was ever mapped in `CustomerEntity`/`AdventureWorksLtDbContext` — there's no login
feature in this API and never has been. The first real integration test exercising
`CustomerWriteRepository.CreateAsync` against the live database surfaced this as a `SqlException`
(`Cannot insert the value NULL into column 'PasswordHash'`), since EF's generated `INSERT` never
supplied a value for a column it doesn't know about.

Two ways to resolve it: map the columns in `CustomerEntity` and populate them with meaningless
placeholder values on every insert, or drop the columns since nothing uses them. Authentication
for this API, if/when it's built, will be handled by an external IdP (Auth0) rather than
DB-stored credentials, so there's no future use case that would need these columns back.

## Decision

Drop `PasswordHash`/`PasswordSalt` from `SalesLT.Customer` via a one-off
`ALTER TABLE SalesLT.Customer DROP COLUMN PasswordHash, PasswordSalt` rather than mapping and
populating them with placeholder values.

## Consequences

- `CustomerWriteRepository.CreateAsync` works with no code changes — `CustomerEntity`'s existing
  (partial) mapping is now a complete match for what the columns require, instead of needing
  placeholder-value plumbing for two columns nothing legitimately uses.
- This schema now diverges from the stock Microsoft AdventureWorksLT sample. If this database is
  ever re-provisioned from a fresh AdventureWorksLT install/restore, both columns come back
  (`NOT NULL`, no default) and `CreateAsync` will fail the same way again — re-run the `ALTER
  TABLE` above if that happens.
- No migrations tooling owns this schema (see ADR-0002/`ef-core-conventions.md`), so this change
  isn't tracked or replayable automatically — it's a manual DDL step, recorded here for anyone
  who needs to reproduce this environment from scratch.
- ADR-0006's mass-assignment-safety example (`PasswordHash`/`PasswordSalt` never appearing in a
  DTO) predates this decision and referenced columns that existed at the time; the DTO-boundary
  reasoning it documents still holds regardless of these specific columns' existence.
