# AHC.Sandbox.Data

## Purpose

The `AHC.Sandbox.Data` project contains the persistence logic for the solution — everything that
talks to the AdventureWorksLT SQL Server database. It implements the repository interfaces that
`AHC.Sandbox.Application` defines.

## Responsibilities

The Data layer is responsible for:

- The EF Core `DbContext` (`AdventureWorksLtDbContext`) and its Fluent API entity mappings
- EF entity classes under `Entities/` (e.g. `CustomerEntity`) — kept deliberately separate from
  the `Domain` entities they map to/from (`Customer`), so EF Core's shape never leaks into
  `Domain`/`Application`
- Repository implementations under `Repositories/`, split per resource into
  `I<Resource>ReadRepository` / `I<Resource>WriteRepository` implementations
- Registering the `DbContext` and repositories in `DependencyInjection.cs`'s `AddData(IConfiguration)`
  extension method, which reads the `ConnectionStrings:AdventureWorksLt` connection string

**No EF migrations tooling is set up** (the `Microsoft.EntityFrameworkCore.Design` package isn't
referenced). `AdventureWorksLtDbContext` maps onto an existing, already-populated database rather
than owning schema creation/versioning — don't introduce `dotnet ef migrations` without checking
with the team first, since that would change what "the schema" means for this project.

## Design Principles

The Data layer should:

- Focus purely on persistence and retrieval — no business rules
- Keep database concerns out of `Domain` and `Application`
- Implement the repository abstractions `Application` defines, rather than defining its own
- Mix two data-access techniques deliberately, not interchangeably. **What decides between them is
  whether `AdventureWorksLtDbContext` maps the tables — not whether the query has joins or
  aggregates:**
  - **EF Core** (`AsNoTracking()` on reads) — the default, for anything over a **mapped** table.
    `SalesLT.Customer`, `SalesLT.Address`, `SalesLT.CustomerAddress`, `SalesLT.Product`,
    `SalesLT.SalesOrderHeader`, and `SalesLT.SalesOrderDetail` are the mapped tables today.
    Aggregates and joins over mapped tables are still EF's job; it handles them fine — see
    `AddressReadRepository` for a LINQ join across two mapped tables and `OrderReadRepository`
    for an `Include` over a parent/child pair with database-computed columns.
  - **Raw parameterized ADO.NET** (`_dbContext.Database.GetDbConnection()` + `DbCommand`) only for
    tables the `DbContext` **doesn't map**, which EF can't see at all — today, just the two
    `Rewards` tables. See `CustomerReadRepository.GetRewardsAsync` for the reference
    pattern (a cross-schema `LEFT JOIN` onto unmapped tables), including the
    open/close-connection-in-`finally` handling, and `.claude/agents/sql-safety-reviewer.md` for
    what "safe" raw SQL looks like here.
  - When a new feature needs an unmapped table, **prefer mapping it and writing LINQ** over adding
    another raw query. `GetRewardsAsync` is the only raw method left — the legacy raw
    customer-order queries were removed with their routes once the order tables were mapped (see
    `docs/adr/0011-consolidate-order-reads-remove-raw-order-sql.md`); don't read raw SQL as the
    answer for aggregates (`GetOrderSummaryAsync` is a LINQ `GroupBy` now).

## Dependencies

This project references:

- `AHC.Sandbox.Application`
- `AHC.Sandbox.Domain`

## Examples

Code that belongs here:

- `AdventureWorksLtDbContext` and its Fluent API mappings
- `CustomerEntity` / `AddressEntity` / `CustomerAddressEntity` and future `<Resource>Entity` types
- `CustomerReadRepository` / `CustomerWriteRepository` / `AddressReadRepository` /
  `AddressWriteRepository` and future repository implementations
- `AddressMapper` — entity → DTO mapping shared by the address read and write repositories, rather
  than duplicated as a private helper in each
- Parameterized raw SQL for queries over tables the `DbContext` doesn't map (today, only the
  `Rewards` tables)

Code that does **not** belong here:

- API controllers
- Business rules that belong on a `Domain` entity
- DTOs (those live in `Application`)
- Non-database external integrations (Redis, email, etc. — those are `Infrastructure`)

## Goal

Isolate all persistence concerns so the database can be queried efficiently (EF over mapped
tables; raw SQL only for unmapped ones) without any of that leaking into `Domain` or `Application`. See
`docs/database-schema.md` for a curated summary of just the tables this project actually touches,
`.claude/skills/adventureworks-schema/SKILL.md` for the full table/column reference across all
three business schemas (`SalesLT`, `SalesIntelligence`, `Rewards`) this project's database has,
and `.claude/agents/api-scaffolder.md` for adding a new resource's Data-layer pieces (including
updating `docs/database-schema.md` in the same pass).
