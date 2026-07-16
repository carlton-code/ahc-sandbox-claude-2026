# AHC.Sandbox

.NET 10 Web API over the **AdventureWorksLT** sample database (`SalesLT` schema), built as a
layered/clean-architecture solution. This file is the project-wide memory for Claude Code; see
`.claude/agents`, `.claude/commands`, `.claude/skills`, and `.claude/rules` for task-specific
helpers. Layer-specific conventions live in `.claude/rules/*.md` (loaded automatically only when
you're working in the matching project) rather than here — see the relevant rule file before
working in a project you haven't touched yet:

| Project | Rule file |
|---|---|
| `AHC.Sandbox.Domain` | `.claude/rules/domain-conventions.md` |
| `AHC.Sandbox.Application` | `.claude/rules/application-conventions.md` |
| `AHC.Sandbox.Data` | `.claude/rules/ef-core-conventions.md` |
| `AHC.Sandbox.Infrastructure` | `.claude/rules/infrastructure-conventions.md` |
| `AHC.Sandbox.Api` | `.claude/rules/api-conventions.md` |
| `AHC.Sandbox.UnitTests` / `AHC.Sandbox.IntegrationTests` | `.claude/rules/testing-conventions.md` |

## Workflow

For changes touching more than one project in this solution, propose a plan in Plan Mode before
editing. A layered architecture means most non-trivial changes span multiple projects (e.g. a new
resource touches `Domain`, `Application`, `Data`, and `Api` together) — plan first so the layering
and scope are agreed before code changes land.

## Solution layout

| Project | Role | Depends on |
|---|---|---|
| `AHC.Sandbox.Domain` | Core entities (e.g. `Customer`), no framework deps | nothing internal |
| `AHC.Sandbox.Application` | DTOs, service interfaces, service implementations (use cases) | `Domain` |
| `AHC.Sandbox.Data` | `AdventureWorksLtDbContext`, EF entities, repositories | `Application`, `Domain` |
| `AHC.Sandbox.Infrastructure` | Cross-cutting technical services (Redis cache, etc.) | `Application`, `Domain` |
| `AHC.Sandbox.Api` | ASP.NET Core controllers, `Program.cs` composition root | all of the above |
| `AHC.Sandbox.UnitTests` | Fast NUnit tests against fakes — `Domain`/`Application` logic only | all of the above |
| `AHC.Sandbox.IntegrationTests` | NUnit tests against real infrastructure (SQL Server and Redis today; end-to-end `Api` not yet) | all of the above |

Dependency direction: `Domain` has no outward dependencies; `Application` defines interfaces and
orchestrates but never references `Data`/`Api`/`Infrastructure` concretely; `Data`/`Infrastructure`
implement those interfaces; `Api` stays thin (controllers delegate to `Application` services, no
business logic in controllers). Each project also has its own `ReadMe-<Project>.md` with the full
purpose/responsibilities/design-principles for that layer.

## Reference vertical slice: Customer

`Customer` is the reference vertical slice — the pattern to copy for every new
resource (see `.claude/agents/api-scaffolder.md`) — `Domain/Entities/Customer.cs` →
`Application/Customers/**` → `Data/Entities/CustomerEntity.cs` +
`Data/Repositories/CustomerReadRepository.cs`/`CustomerWriteRepository.cs` →
`Api/Controllers/CustomersController.cs`. Each layer's rule file (table above) points to the exact
files to look at for that layer specifically.

The API's top-level resources today are `Customer` and `Product` (`Product` was built by copying
this pattern; `Customer` remains the richer reference). They aren't the only slices: customer
addresses are a read-only sub-resource with their own Application/Data pieces
(`Application/Addresses/**`, `Data/Entities/AddressEntity.cs`/`CustomerAddressEntity.cs`,
`Data/Repositories/AddressReadRepository.cs`), exposed through `CustomersController`. See
`docs/database-schema.md` for a curated summary of the tables this codebase actually touches, and
`.claude/skills/adventureworks-schema` for the full verified column-level reference across every
schema if you need more than that summary.

## Dependency injection convention

Every layer exposes a single `Add<LayerName>()` extension method in its own `DependencyInjection.cs`
(`AddApplication()`, `AddData(IConfiguration)`, `AddInfrastructure(IConfiguration)`). `Program.cs` composes
these plus `AddControllers()`/`AddOpenApi()`. New services/repositories get registered inside the
extension method for the layer that implements them, not in `Program.cs` directly.

## Architecture Decision Records

`docs/adr/` has one short file per significant architectural decision (Clean Architecture over
vertical slices, EF Core over Dapper, NUnit over xUnit/MSTest, Redis over `IMemoryCache`, the
unit/integration test split, DTOs as the API boundary — see `docs/adr/README.md` for the full
index). Read the relevant ADR before proposing to change something it covers, so the original
trade-off gets weighed again rather than silently reversed — and add a new ADR (see
`docs/adr/template.md`) for any future decision of similar weight.
