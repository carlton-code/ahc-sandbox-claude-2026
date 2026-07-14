---
paths:
  - "src/AHC.Sandbox.Api/**/*"
---

# API-layer conventions

- Route pattern: `api/v1/[controller]` (lowercase plural resource names).
- Controllers depend only on an Application-layer service interface (e.g. `ICustomerService`) —
  never on a repository or `AdventureWorksLtDbContext` directly.
- Bind requests/responses to DTOs only — never an EF entity or the `Domain` type directly. See
  `docs/adr/0006-dtos-as-the-api-boundary.md` for why (mass-assignment/over-posting safety).
- Not-found semantics: `NotFound()` when a nullable lookup result is `null`; `NoContent()` on a
  successful mutation, `NotFound()` if the mutation's target doesn't exist; `POST` returns
  `CreatedAtAction`. See `CustomersController.cs` for the reference.
- **No authentication/authorization scheme is configured yet**, despite `Program.cs` calling
  `UseAuthorization()` — every endpoint is currently open.
- `docs/api.md` documents the HTTP surface (routes, DTOs, status codes) and must be updated in
  the same pass as any controller change — see `.claude/agents/docs-writer.md`.

## Current state

`CustomersController.cs` is the only controller in this project and is the reference pattern to
copy.
