# ADR-0005: Split test projects into `UnitTests` and `IntegrationTests`

## Status

Accepted

## Context

The solution originally had a single `AHC.Sandbox.Tests` project referencing every `src`
project. That's fine as long as every test is fast and fake-based, but this codebase already has
code that can only be tested meaningfully against real infrastructure — the raw ADO.NET
repository methods (`CustomerReadRepository.GetOrderSummaryAsync` etc.) need a real SQL Server,
and the planned Redis caching layer (ADR-0004) will need a real Redis instance. Mixing both kinds
of test in one project means either the fast tests start depending on real infrastructure being
available, or the infrastructure-dependent tests get skipped/faked in a way that stops testing
anything real.

## Decision

Split into two projects:

- **`AHC.Sandbox.UnitTests`** — fast, isolated tests against fakes. No real database, no real
  Redis, no network.
- **`AHC.Sandbox.IntegrationTests`** — tests that need real infrastructure: a real SQL Server for
  `Data`'s raw-SQL methods, eventually a real Redis instance for `Infrastructure`'s cache, and/or
  full end-to-end `Api` tests.

Both use the same NUnit stack (see ADR-0003) and reference every `src` project, for consistency.

## Consequences

- Two test projects to keep in sync (same package versions, same conventions) instead of one.
- A judgment call is required on where a new test belongs — documented in both projects'
  `ReadMe-*.md` files and in `.claude/agents/test-runner.md`'s "which project a new test goes in"
  guidance: if a fake can exercise it, it's a unit test; if it fundamentally needs the real thing,
  it's an integration test.
- `AHC.Sandbox.IntegrationTests` is scaffolding only as of this decision — no connection
  configuration is wired up yet. The first real integration test needs to add that rather than
  assume it already exists.
- `dotnet test` from the repo root still runs both; either can be run in isolation with
  `dotnet test tests/AHC.Sandbox.UnitTests` / `tests/AHC.Sandbox.IntegrationTests`, which matters
  once integration tests need infrastructure that isn't always available (e.g. in a quick local
  loop without Docker/SQL Server running).
