---
paths:
  - "src/AHC.Sandbox.Data/**/*"
---

# Data-layer / EF Core conventions

Repositories mix two techniques on purpose. **The line between them is whether this `DbContext`
maps the tables involved — not whether the query has aggregates or joins.**

1. **EF Core** (`AsNoTracking()` for reads) — the default, for anything over a **mapped** table.
   `SalesLT.Customer`, `SalesLT.Address`, `SalesLT.CustomerAddress`, `SalesLT.Product`,
   `SalesLT.SalesOrderHeader`, and `SalesLT.SalesOrderDetail` are the mapped tables today. See
   `CustomerReadRepository.GetAllAsync` / `GetByIdAsync` for plain reads, `SearchByNameAsync` for
   a more involved one (`EF.Functions.Like` over computed concatenations, with wildcard
   escaping), `AddressReadRepository` for a LINQ join across two mapped tables, and
   `OrderReadRepository` for an `Include` over a parent/child pair with database-computed
   columns (`ValueGeneratedOnAddOrUpdate()`).
2. **Raw ADO.NET** via `_dbContext.Database.GetDbConnection()` + parameterized `DbCommand` — for
   queries against tables the `DbContext` **doesn't map**, which EF therefore can't see at all:
   today, only the two `Rewards` tables. `GetRewardsAsync` in `CustomerReadRepository.cs` is the
   reference: a cross-schema `LEFT JOIN` from a mapped table onto two unmapped ones. See
   `docs/adr/0002-ef-core-over-dapper.md` for why raw ADO.NET rather than Dapper.

**Don't reach for raw SQL by default — only when a plain EF LINQ query genuinely can't express it.**
In practice that means: if the tables are already mapped, LINQ is the answer, aggregates and joins
included — EF handles those fine. If a new feature needs an unmapped table, **prefer mapping the
table and writing LINQ over adding another raw query.** Raw SQL is where you land when mapping
isn't worth it (a one-off cross-schema report, say), not the default for anything that looks
SQL-ish.

The existing raw customer-order queries (`GetOrdersByCustomerIdAsync`, `GetOrderByIdAsync`,
`GetRecentOrdersAsync`, `GetOrderSummaryAsync`) predate `SalesLT.SalesOrderHeader` being mapped
and are **grandfathered deliberately** — see `docs/adr/0010-map-order-tables-keep-legacy-raw-reads.md`.
They work, they're covered by integration tests, and migrating them to LINQ is a recorded
follow-up decision, not something to fold into other work. Don't read them as a template saying
"aggregates mean raw SQL," and don't extend them: **new order querying goes through the mapped
entities and LINQ** (`OrderReadRepository` is the reference).

When adding raw SQL: always parameterize (`AddParameter` helper — never string-concatenate
input), and follow the existing open/close-connection-in-`finally` pattern rather than assuming
the EF connection is already open. See `.claude/agents/sql-safety-reviewer.md` for the full
checklist.

**No EF migrations tooling is set up** (`Microsoft.EntityFrameworkCore.Design` isn't referenced)
— `AdventureWorksLtDbContext` maps onto an existing, already-populated database rather than
owning schema creation/versioning. Don't introduce `dotnet ef migrations` without checking with
the user first.

## Configuration

Connection string key: `ConnectionStrings:AdventureWorksLt`, read in `AddData(IConfiguration)` in
`DependencyInjection.cs`. Register new repositories in that same extension method — not in
`Program.cs`.

## Schema accuracy

New EF entity mappings (`ToTable`, `HasColumnName`, `HasMaxLength`, `IsRequired`, etc.) must match
the real database schema. Check `docs/database-schema.md` for a curated summary of the tables
this codebase actually touches, or `.claude/skills/adventureworks-schema/SKILL.md` for the full
verified column-level reference — don't guess column names or nullability.

## Reference

`Entities/CustomerEntity.cs` + `Context/AdventureWorksLtDbContext.cs` (Fluent API mapping onto
`SalesLT.Customer`) and `Repositories/CustomerReadRepository.cs` /
`CustomerWriteRepository.cs` are the current example of this layer's shape.
