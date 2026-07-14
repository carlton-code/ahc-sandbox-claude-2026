# ADR-0002: EF Core + raw ADO.NET over adding Dapper

## Status

Accepted

## Context

`AHC.Sandbox.Data` needs to both (a) do simple CRUD against single mapped tables and (b) run
multi-column aggregate/join queries (e.g. order summaries against `SalesLT.SalesOrderHeader`)
that don't map cleanly onto one tracked EF entity. A common .NET pattern for exactly this split is
EF Core for the CRUD side plus Dapper for the reporting/aggregate side, since Dapper is a
lightweight micro-ORM built around exactly that "hand-write the SQL, get results mapped to a
type" need.

## Decision

Use EF Core as the only ORM, and drop to **raw parameterized ADO.NET** (via
`_dbContext.Database.GetDbConnection()` + `DbCommand`, reusing the `DbContext`'s own connection)
for the queries that don't fit EF's tracked-entity model — instead of adding Dapper as a second
library for that role. See `CustomerReadRepository.GetOrderSummaryAsync` /
`ExecuteOrderQueryAsync` for the pattern.

Dapper's main value over raw ADO.NET is convenient automatic mapping from a data reader to a
type. This codebase already hand-maps each DTO field by field in the raw-SQL repository methods,
so that specific convenience wasn't the deciding factor — and reusing the connection EF Core
already manages avoids introducing a second query/connection-management convention to learn and
keep consistent alongside EF's.

## Consequences

- Raw-SQL repository methods hand-map every column from `IDataReader` to a DTO manually (see the
  `Convert.To*`/`is DBNull` checks in `ExecuteOrderQueryAsync`) — more verbose per query than
  Dapper's `query.QueryAsync<T>()` would be.
- One fewer NuGet package/convention to keep consistent across the `Data` project.
- If manual field-by-field mapping becomes a recurring pain point as more resources add
  aggregate/reporting queries, Dapper is the natural thing to reconsider at that point — this
  decision doesn't rule it out forever, it just explains why it wasn't reached for initially.
- See `.claude/agents/sql-safety-reviewer.md` for the safety rules (parameterization, connection
  lifecycle) that apply to every instance of this raw-SQL pattern.
