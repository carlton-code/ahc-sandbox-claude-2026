# AHC.Sandbox.Api

## Purpose

The `AHC.Sandbox.Api` project is the ASP.NET Core Web API host for the solution — the only host
project that exists today (there's no separate Web UI or CLI in this solution). It exposes the
application's use cases as HTTP endpoints over the AdventureWorksLT sample database.

## Responsibilities

The API layer is responsible for:

- Defining HTTP endpoints (controllers under `Controllers/`)
- Accepting requests and returning responses
- Model binding via request DTOs (never binding directly to an EF entity or domain type)
- API-specific middleware and filters
- OpenAPI / Swagger configuration
- Translating HTTP input into Application-layer service calls
- Composing the application at startup (`Program.cs` calls `AddApplication()`, `AddData()`,
  `AddInfrastructure()` from the other layers, then builds and runs the host)

**Not yet implemented:** authentication/authorization. `Program.cs` calls `UseAuthorization()`,
but no authentication scheme is configured — every endpoint is currently open. Add a scheme
before this API is exposed anywhere it matters.

## Design Principles

The API layer should:

- Be thin and focused on HTTP concerns
- Delegate business logic to the Application layer — controllers depend only on an Application
  service interface (e.g. `ICustomerService`), never on a repository or `DbContext` directly
- Avoid placing business rules directly in controllers
- Return clear, consistent API responses — see the status-code convention below
- Handle cross-cutting HTTP concerns (middleware, exception handling, logging configuration) here,
  not in Application

### Conventions actually in use

- Route pattern: `[Route("api/v1/[controller]")]`.
- Status codes: `NotFound()` when a nullable lookup returns `null`; `NoContent()` on a successful
  mutation; `NotFound()` on a mutation whose target doesn't exist; `CreatedAtAction` on `POST`.
- A write that violates a database constraint surfaces as `409 Conflict`, translated by
  `Infrastructure/DatabaseConflictExceptionHandler.cs` (this project's `Infrastructure/` folder,
  not the `AHC.Sandbox.Infrastructure` project) — controllers don't catch it themselves. See
  `docs/adr/0009-customer-delete-refuses-rather-than-cascades.md`.
- Actions with a response body return `ActionResult<T>`; body-less ones (`PUT`/`DELETE`) return
  `IActionResult`.
- Every status an action can return is declared with `[ProducesResponseType]`, **including the
  success one** — adding any such attribute replaces the framework's inferred `200` instead of
  adding to it, so a partially-annotated action documents its `404` and silently loses its `200`.
- See `CustomersController` for the reference implementation of all of the above.

## Dependencies

This project references:

- `AHC.Sandbox.Application`
- `AHC.Sandbox.Data`
- `AHC.Sandbox.Domain`
- `AHC.Sandbox.Infrastructure`

As the outermost layer, `Api` is the one project allowed to reference everything else — it's the
composition root.

## Examples

Code that belongs here:

- `Program.cs` — composition root and middleware pipeline
- Controllers — `CustomersController` is the only one today (fully implemented, the reference
  pattern to copy).
- `Infrastructure/DatabaseConflictExceptionHandler.cs` — cross-cutting HTTP concern: translates
  database constraint violations into `409` responses instead of unhandled 500s.

Code that does **not** belong here:

- Core domain entities used directly as request/response contracts (use a DTO instead)
- EF Core mappings
- Repository logic
- Business workflows implemented directly in controllers

## Goal

Provide a clean, thin HTTP interface into the application while keeping transport concerns
separate from the business and persistence logic living in the other layers. See `docs/api.md`
for the actual documented HTTP surface (kept in sync by
`.claude/agents/docs-writer.md`), `.claude/skills/clean-architecture/SKILL.md` for why the
transport/business separation matters, and `.claude/agents/api-scaffolder.md` for how to add a
new resource.
