# AHC.Sandbox.IntegrationTests

## Purpose

The `AHC.Sandbox.IntegrationTests` project contains tests that need real infrastructure to mean
anything — a real SQL Server against the AdventureWorksLT database, a real Redis instance, and/or
a running `Api` host. It's the counterpart to `AHC.Sandbox.UnitTests`, which covers
`Domain`/`Application` logic in isolation with fakes.

**Current state:** this project has a checked-in `appsettings.json` (`ConnectionStrings:AdventureWorksLt`
plus a `Redis` section) copied to the output directory, and `Infrastructure/TestConfiguration.cs`,
`Infrastructure/DbContextTestFactory.cs`, and `Infrastructure/RedisTestFixture.cs` load it into a
real `AdventureWorksLtDbContext` / `IConnectionMultiplexer` the same way `AHC.Sandbox.Data`'s
`AddData` and `AHC.Sandbox.Infrastructure`'s `AddInfrastructure` do, minus DI. `Customers/`,
`Addresses/`, `Products/`, `Orders/`, and `Caching/` contain the real tests built on top of those fixtures. There is no
`WebApplicationFactory<Program>`-based end-to-end `Api` test yet — add
`Microsoft.AspNetCore.Mvc.Testing` when that's worth the setup.

## Responsibilities

This project is responsible for:

- Testing `Data` repository behavior against a real SQL Server — especially the raw ADO.NET
  methods in `CustomerReadRepository` (`GetOrderSummaryAsync`, `ExecuteOrderQueryAsync`) that a
  fake repository can't meaningfully exercise, since their whole job is running real SQL
- Testing `Infrastructure`'s Redis caching against a real Redis instance —
  `Caching/RedisCustomerCacheRepositoryTests.cs` and `Caching/RedisProductCacheRepositoryTests.cs`
  on top of `Infrastructure/RedisTestFixture.cs`
  (see `.claude/agents/redis-cache-builder.md` for the pattern under test)
- Optionally, full end-to-end `Api` tests (real HTTP request → real controller → real database)
  where that's more valuable than testing a repository in isolation

## What belongs here vs. `AHC.Sandbox.UnitTests`

| | `IntegrationTests` | `UnitTests` |
|---|---|---|
| Speed | Seconds, hits real infrastructure | Milliseconds, no I/O |
| Dependencies under test | `Data` (real SQL Server), `Infrastructure` (real Redis), `Api` end-to-end | `Domain`, `Application` |
| Repository access | The real `Data`/`Infrastructure` implementations | Fake/in-memory implementations |
| Run cadence | Before a commit / in CI, when real infrastructure is available | Every build, every save |

If a test can run with a fake repository and no real database/Redis/network, it belongs in
`AHC.Sandbox.UnitTests` instead — don't reach for this project by default just because "it's an
integration" in the loose sense.

## Design Principles

- Only put a test here if it genuinely needs real infrastructure — otherwise it belongs in
  `AHC.Sandbox.UnitTests`
- Keep these tests runnable against a local dev database/Redis instance; don't hard-code
  assumptions that only hold in one machine's environment
- Expect these to be slower and less frequently run than unit tests — that's fine, that's the
  tradeoff for testing the real thing
- Make failures easy to diagnose even though more moving parts are involved (real connection,
  real data) — assert on what the test actually cares about, not incidental database state

## Dependencies

This project references every `src` project:

- `AHC.Sandbox.Domain`
- `AHC.Sandbox.Application`
- `AHC.Sandbox.Data`
- `AHC.Sandbox.Infrastructure`
- `AHC.Sandbox.Api`

Plus NUnit, `NUnit3TestAdapter`, `NUnit.Analyzers`, `Microsoft.NET.Test.Sdk`, and
`coverlet.collector` — the same NUnit stack as `AHC.Sandbox.UnitTests`, for consistency. Add
`Microsoft.AspNetCore.Mvc.Testing` when the first full end-to-end `Api` test is written.

## Examples

Code that belongs here:

- `Customers/CustomerReadRepositoryTests.cs` — `CustomerReadRepository` (including the raw-SQL
  order-summary/order-query methods) against the real database.
- `Customers/CustomerWriteRepositoryTests.cs` — `CustomerWriteRepository` inserts/updates/deletes
  against the real database, each test cleaning up its own throwaway row.
- `Addresses/AddressReadRepositoryTests.cs` — `AddressReadRepository`'s customer-scoped address
  reads (ordering, field mapping, cross-customer scoping) against the real database.
- `Products/ProductReadRepositoryTests.cs` — `ProductReadRepository`'s seed-based reads (column
  mapping including the money/decimal(8,2)/datetime store types, name ordering) against the real
  database.
- `Products/ProductWriteRepositoryTests.cs` — `ProductWriteRepository` inserts/updates/deletes
  against the real database, including the unique-`ProductNumber` and delete-with-order-lines
  constraint paths, each test cleaning up its own throwaway row.
- `Orders/OrderReadRepositoryTests.cs` — `OrderReadRepository`'s seed-based reads (newest-first
  ordering against a raw-SQL ground truth, header and line column mapping including the
  database-computed `SalesOrderNumber`/`TotalDue`/`LineTotal`) against the real database.
- `Caching/RedisCustomerCacheRepositoryTests.cs` / `RedisProductCacheRepositoryTests.cs` — the
  Redis cache repositories against a real local Redis instance (`localhost:6379`, per this
  project's `appsettings.json`), including the cache-unavailable → `CacheUnavailableException`
  path.
- End-to-end controller tests via `WebApplicationFactory<Program>`, if/when that's worth the setup.

## Goal

Cover the things a unit test fundamentally can't — real SQL execution, real Redis behavior, real
end-to-end request handling — without slowing down the fast feedback loop `AHC.Sandbox.UnitTests`
provides. See `.claude/agents/test-runner.md` for building and running the test suite, and
`.claude/agents/sql-safety-reviewer.md`/`.claude/agents/redis-cache-builder.md` for the
correctness rules this project's tests should be verifying against real infrastructure.
