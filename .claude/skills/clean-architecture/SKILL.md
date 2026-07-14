---
name: clean-architecture
description: Explains Clean Architecture's dependency rule and layer responsibilities, and maps them onto this solution's five projects. Use when deciding which project a piece of new code belongs in, when a change seems to cross a layer boundary it shouldn't, or when explaining/teaching why this solution is structured the way it is.
---

# Clean Architecture in AHC.Sandbox

## The core idea: the Dependency Rule

Source code dependencies point in one direction only: **inward**. Inner layers know nothing
about outer layers — not their types, not their frameworks, not their existence.

Concentric rings, outer to inner:

1. **Frameworks & Drivers** — ASP.NET Core, EF Core, SQL Server, Redis, StackExchange.Redis.
   The most volatile, most replaceable layer.
2. **Interface Adapters** — controllers, repositories, cache implementations. Translate between
   the outside world's shapes (HTTP requests, SQL rows, Redis strings) and the inner rings'
   shapes (DTOs, domain entities).
3. **Use Cases** — application-specific business logic: what happens when a particular request
   comes in, expressed in terms the business would recognize.
4. **Entities** — enterprise-wide business rules and data, with zero knowledge of how they're
   stored, transported, or displayed.

Business rules (rings 3–4) never depend on delivery or persistence mechanisms (rings 1–2) — the
outer rings depend on the inner ones, never the reverse. Crossing a ring boundary happens through
an interface *owned by the inner ring* and implemented by the outer ring (dependency inversion,
a.k.a. "ports and adapters"): the inner ring defines the port, the outer ring plugs in as an
adapter.

## How this maps onto AHC.Sandbox's projects

| Ring | Project | Role |
|---|---|---|
| Entities | `AHC.Sandbox.Domain` | Plain business objects (`Customer`), zero framework references. |
| Use Cases | `AHC.Sandbox.Application` | Services (`CustomerService`) orchestrating ports; DTOs as the boundary shape; the ports themselves (`ICustomerReadRepository`, `ICustomerWriteRepository`, `ICustomerService`) are declared here, not in the layer that implements them. |
| Interface Adapters / Frameworks | `AHC.Sandbox.Data` | Adapter implementing the persistence ports — EF Core + raw ADO.NET against SQL Server. A swappable detail: Application doesn't know or care that it's SQL Server underneath. |
| Interface Adapters / Frameworks | `AHC.Sandbox.Infrastructure` | Adapter implementing technical ports that aren't the primary database — Redis caching, eventually email/storage/etc. Also a swappable detail. |
| Interface Adapters | `AHC.Sandbox.Api` | The delivery mechanism (HTTP/ASP.NET Core). Could be replaced by a console app, a gRPC service, or a message consumer without Application or Domain changing at all. |
| (cross-cutting) | `AHC.Sandbox.UnitTests` | Exercises `Domain`/`Application` in isolation against fakes, which is only possible *because* ring boundaries are interfaces — this is the payoff for the discipline above, not a separate ring. |
| (cross-cutting) | `AHC.Sandbox.IntegrationTests` | Exercises the adapters (`Data`, `Infrastructure`, `Api`) against the real frameworks/drivers they wrap — the one place where testing the outermost ring for real is the point. |

The project reference graph enforces the dependency rule at compile time: `Domain` has no
internal references; `Application` references only `Domain`; `Data`/`Infrastructure` reference
`Application` + `Domain` (to implement the ports); `Api` references everything (it's the
outermost ring, composing the others together in `Program.cs`).

## Why the read/write repository split exists

Every resource in this solution splits its repository port into `I<Resource>ReadRepository` and
`I<Resource>WriteRepository` instead of one combined interface. This is a lightweight
CQRS-flavored convention (not full CQRS — no separate event store or message bus):

- Read paths return DTO-shaped projections and can drop into raw SQL for aggregates/joins
  without that leaking into the write path's concerns.
- A service or test can depend on just the read side (or just the write side) without dragging
  in the other.
- It keeps each interface small and single-purpose, which is easier to fake in a test than one
  repository interface with a dozen unrelated methods.

## Practical decision guide: "where does this code go?"

- **A rule that would be true no matter what database or UI existed** (e.g. "a customer's full
  name is first + middle + last") → `Domain`, on the entity itself.
- **Orchestration of a specific use case** ("when a create-customer request comes in, do X then
  Y") → `Application`: a service method, plus whatever DTOs/ports it needs.
- **Talking to the primary SQL Server database, or mapping an entity to a table** → `Data`.
- **Talking to any other external system** (Redis, email, file storage, a third-party API) →
  `Infrastructure`.
- **HTTP concerns** — routes, model binding, status codes → `Api`, and nothing else.
- **Never**: `Domain` referencing anything internal; `Application` referencing `Data`,
  `Infrastructure`, or `Api` concretely; business logic living in a controller; `Data` or
  `Infrastructure` containing business rules instead of just implementing a port.

## Smells to watch for in this codebase specifically

- **Anemic domain model drift.** `Domain.Customer` is currently a plain data holder (with one
  computed property, `FullName`) — fine for a CRUD-heavy API today, but if real business rules
  show up later (discount eligibility, order validation, rewards-tier logic), they belong on the
  domain entity or a domain service, not scattered across `Application` services or, worse, a
  controller.
- **`CustomerEntity` and `Customer` merging back into one class.** They're deliberately separate
  — `CustomerEntity` (Data) is an EF Core–shaped persistence model, `Customer` (Domain) is the
  business model. Collapsing them "to save a class" re-couples Domain to EF Core, which is
  exactly what the split exists to prevent.
- **A `DbContext` or `IConnectionMultiplexer` reaching outside its owning layer.** Never inject
  `AdventureWorksLtDbContext` into `Application` or `Api`; never let a controller talk to Redis
  directly.
- **New service registrations landing in `Program.cs`** instead of the owning layer's own
  `Add<Layer>()` extension method — breaks the idea that each ring wires up its own adapters.
- **A port interface defined in the wrong ring.** Watch for a new interface being added directly
  under `Infrastructure` (or any outer-ring project) instead of `Application` — `Application`
  can't depend on something it can't see. `ICustomerCacheRepository`
  (`Application/Customers/Interfaces/`, implemented by `Infrastructure`'s
  `RedisCustomerCacheRepository`) is the reference example of getting this right.

## Related

- Root `CLAUDE.md` — the cross-cutting, repo-specific conventions (solution layout, DI extension
  method pattern) built on top of these principles.
- `.claude/rules/*.md` — the per-layer conventions (EF Core, API, testing, etc.) that load
  automatically once you're working in the matching project.
- `.claude/agents/architecture-reviewer.md` — actively checks a diff against the dependency rule
  and these conventions.
- `.claude/agents/api-scaffolder.md` — builds new vertical slices following this layering.
- `.claude/agents/redis-cache-builder.md` — the reasoning behind the `Customer` cache's
  port-in-`Application`/adapter-in-`Infrastructure` shape, and the pattern to follow when
  extending caching to another resource.
- Each project's own `ReadMe-<Project>.md` — the most detailed, project-specific version of
  "what belongs here."
