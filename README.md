# AHC.Sandbox

A .NET 10 Web API sandbox built over Microsoft's **AdventureWorksLT** sample database, used to
experiment with a clean, layered API architecture (and, more recently, an AI-assisted development
workflow via Claude Code — see [Working with Claude Code on this project](#working-with-claude-code-on-this-project)
below).

## What this is

An ASP.NET Core Web API over the `SalesLT` schema of AdventureWorksLT, plus two custom schemas —
`SalesIntelligence` and `Rewards` — that extend the sample database with product bundles,
recommendations, and a customer rewards program. `Customer` (with CRUD and order-reporting
endpoints) is the only resource in the API today.

## Solution layout

| Project | Role |
|---|---|
| `src/AHC.Sandbox.Domain` | Core business entities (`Customer`, ...). No framework dependencies. |
| `src/AHC.Sandbox.Application` | Use cases: DTOs, service interfaces, service implementations. Depends only on `Domain`. |
| `src/AHC.Sandbox.Data` | EF Core `DbContext`, entity mappings, repository implementations. Talks to SQL Server. |
| `src/AHC.Sandbox.Infrastructure` | Cross-cutting technical services (Redis caching config — scaffolded, not yet wired in). |
| `src/AHC.Sandbox.Api` | ASP.NET Core host: controllers, `Program.cs`, Swagger/OpenAPI. |
| `tests/AHC.Sandbox.UnitTests` | Fast NUnit tests against fakes — no real database or Redis. |
| `tests/AHC.Sandbox.IntegrationTests` | NUnit tests against real infrastructure (SQL Server, eventually Redis, end-to-end `Api`). Scaffolding only today. |

Dependencies flow one direction: `Api` → `Application`/`Data`/`Infrastructure` → `Domain`. Each
project has its own `ReadMe-<Project>.md` with the detailed purpose/responsibilities/design
principles for that layer — read the relevant one before adding code to a layer you haven't
touched yet.

## Why things are built this way

`docs/adr/` has a short Architecture Decision Record for each significant choice behind this
solution — Clean Architecture over vertical slices, EF Core over Dapper, NUnit over xUnit/MSTest,
Redis over `IMemoryCache`, the unit/integration test split, DTOs as the API boundary. Worth
reading before proposing to change any of them — see `docs/adr/README.md` for the index and the
convention for adding a new one.

`Customer` is the reference implementation to copy when building out a new resource — it has a
full slice through every layer (`Domain/Entities/Customer.cs` → `Application/Customers/**` →
`Data/Repositories/CustomerReadRepository.cs`/`CustomerWriteRepository.cs` →
`Api/Controllers/CustomersController.cs`).

## Database

Connects to a SQL Server instance running AdventureWorksLT — connection string lives under
`ConnectionStrings:AdventureWorksLt` in `src/AHC.Sandbox.Api/appsettings.json` /
`appsettings.Development.json`. There's no EF migrations tooling in this solution; the
`DbContext` maps onto an existing database rather than creating/versioning one.

Beyond the stock `SalesLT` schema, this particular database also has:

- **`SalesIntelligence`** schema — `Bundle`, `BundleProduct`, `ProductRecommendations`,
  `CustomerRecommendations`
- **`Rewards`** schema — `RewardsLevel`, `CustomerRewardsLevel`

Neither is consumed by the API yet — they represent the next logical features to build (product
bundles, recommendations, a rewards program). See
[`docs/database-schema.md`](docs/database-schema.md) for a short, curated summary of just the
tables this codebase actually touches (a better starting point than the full schema), and
[`.claude/skills/adventureworks-schema/SKILL.md`](.claude/skills/adventureworks-schema/SKILL.md)
for the full verified column-by-column reference across every schema, including the two stock
`SalesLT` columns (`Product.CurrentDiscount`, `SalesOrderHeader.TrackingNumber`) that were added
on top of the public sample.

## Running it

```
dotnet build
dotnet run --project src/AHC.Sandbox.Api
```

In Development, Swagger UI is available at `/openapi/v1.json` via the running host. Tests use
NUnit:

```
dotnet test
```

## Working with Claude Code on this project

This repo has a `.claude/` configuration folder and a root `CLAUDE.md` that teach
[Claude Code](https://claude.com/claude-code) this project's specific architecture and
conventions, so it can act as a well-briefed contributor instead of guessing from scratch each
session. None of this is required to build or run the solution — it's tooling for anyone using
Claude Code to work in this repo, whether they're brand new to the project or have been on it for
months.

### `CLAUDE.md` (root)

Automatically loaded at the start of every Claude Code session in this repo. It's the project's
cross-cutting "memory": the solution layout, the `Customer` vertical-slice pattern, the DI
registration convention, and the ADR pointer. Layer-specific conventions (EF Core, API, testing,
etc.) live in `.claude/rules/` instead — see below.

### Rules (`.claude/rules/`)

Path-scoped conventions that only load when Claude is actually working with files in the matching
project, instead of every session regardless of relevance:

| Rule file | Loads when working in |
|---|---|
| `domain-conventions.md` | `src/AHC.Sandbox.Domain/` |
| `application-conventions.md` | `src/AHC.Sandbox.Application/` |
| `ef-core-conventions.md` | `src/AHC.Sandbox.Data/` |
| `infrastructure-conventions.md` | `src/AHC.Sandbox.Infrastructure/` |
| `api-conventions.md` | `src/AHC.Sandbox.Api/` |
| `testing-conventions.md` | `tests/` (both test projects) |

Each one holds the same kind of detail `CLAUDE.md` used to carry directly (the EF-Core-vs-raw-SQL
convention, API route/status-code conventions, the unit/integration test split, and so on) —
moved here so `CLAUDE.md` stays short and every session doesn't load conventions for layers it
isn't touching.

### Agents (`.claude/agents/`)

Specialized assistants you can hand a task to by name (e.g. "use the api-scaffolder agent to
build out the Products endpoint"), each scoped to one job:

| Agent | Use it for |
|---|---|
| `api-scaffolder` | Building a brand-new CRUD resource across all five layers, following the `Customer` pattern. |
| `code-reviewer` | A general correctness/quality pass — logic bugs, null/async handling, EF Core query correctness, API/DTO security hygiene, test-coverage gaps, and duplication against this codebase's own patterns. Complements the two reviewers below rather than overlapping them. |
| `architecture-reviewer` | Checking a change against the solution's layering rules (dependency direction, DI placement, controller thinness) before it's committed. |
| `sql-safety-reviewer` | Reviewing any new raw ADO.NET/SQL code for parameterization, schema correctness, and connection-lifecycle bugs. |
| `test-runner` | Building, running the NUnit suite, diagnosing failures, and writing new tests in the existing style. |
| `redis-cache-builder` | Building out the Redis caching layer in `AHC.Sandbox.Infrastructure` — explains the reasoning behind each caching decision (cache-aside, TTLs, key naming, failure fallback), not just the code. |
| `docs-writer` | Keeping `docs/api.md` in sync after any controller change — verifies against the live-generated OpenAPI document rather than trusting a code read alone. |

Claude Code also ships a built-in `/code-review` command that isn't part of this folder — it's a
good generic pass on any repo. The `code-reviewer` agent above exists alongside it because it
knows this codebase's specific patterns (its DTO/entity split, its existing duplication, its
nullability landmines) in a way a generic review can't.

### Slash commands (`.claude/commands/`)

Quick, repeatable prompts you invoke directly in a Claude Code session:

- **`/new-resource <name> <table>`** — scaffolds a new CRUD resource end-to-end (e.g.
  `/new-resource Product SalesLT.Product`).
- **`/verify`** — builds, runs the test suite, and reviews the pending changes against the
  architecture and SQL-safety checklists in one pass. A good habit before committing anything
  non-trivial.

### Skills (`.claude/skills/`)

Reference material and repeatable task walkthroughs Claude loads automatically when relevant:

- **`adventureworks-schema`** — a verified, column-level map of every table and view in this
  database, including the two custom schemas (`SalesIntelligence`, `Rewards`) mentioned above.
  Kept up to date by re-running the `INFORMATION_SCHEMA` queries documented at the top of that
  file if the database ever changes — worth doing that refresh (or asking Claude to) after any
  schema migration.
- **`clean-architecture`** — explains the Dependency Rule and how it maps onto this solution's
  five projects, a practical "where does this code go?" decision guide, and specific
  layering smells to watch for in this codebase. Worth reading directly (not just letting Claude
  use it) if you want the reasoning behind why the solution is shaped the way it is.
- **`nuget-package-audit`** — walks through `dotnet list package --outdated`/`--vulnerable`
  and applies this project's bump-vs-hold policy: always bump on a vulnerability, bump routinely
  for patches and test-only tooling, verify before calling a runtime-package minor bump done, hold
  on majors/prereleases, and keep certain packages (EF Core + its SQL Server provider, the NUnit
  stack across both test projects) in lockstep rather than letting them drift apart.
- **`run-api`** — how to start, verify, and cleanly stop the API on this Windows/Git Bash setup
  for manual testing. The one non-obvious part: the shell's own PID for a backgrounded
  `dotnet run` isn't the Windows process actually holding the port/build lock, so a plain `kill`
  doesn't work and a wildcard `taskkill` is too blunt (and gets blocked) — this skill has the
  exact PowerShell one-liner to find and stop the right process.

### Keeping this current

If you add a new layer convention, a new agent-worthy workflow, or the database schema changes,
update `CLAUDE.md` / the relevant `.claude/` file in the same PR as the code change — this is
living documentation, not a one-time setup, and it goes stale exactly like any other doc if it's
left behind.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for branch naming, commit message format, and the
pre-merge/pre-PR checklist — including which of the agents above to run before calling a change
done.
