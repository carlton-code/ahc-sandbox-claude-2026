---
paths:
  - "src/AHC.Sandbox.Data/**/*"
---

# Data-layer / EF Core conventions

Repositories mix two techniques on purpose:

1. **EF Core** (`AsNoTracking()` for reads) for simple CRUD against a single mapped entity — see
   `CustomerReadRepository.GetAllAsync` / `GetByIdAsync`.
2. **Raw ADO.NET** via `_dbContext.Database.GetDbConnection()` + parameterized `DbCommand`, for
   multi-column aggregates/joins that don't map cleanly to a tracked entity (e.g. order summaries
   against `SalesLT.SalesOrderHeader`) — see `GetOrderSummaryAsync` / `ExecuteOrderQueryAsync` in
   `CustomerReadRepository.cs`. Don't reach for raw SQL by default — only when a plain EF LINQ
   query genuinely can't express it. See `docs/adr/0002-ef-core-over-dapper.md` for why this is
   raw ADO.NET rather than Dapper.

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
