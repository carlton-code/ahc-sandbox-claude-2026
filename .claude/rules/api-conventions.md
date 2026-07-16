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
- **Return `ActionResult<T>`** from any action with a response body (not bare `IActionResult`), so
  the compiler checks the success type against the method body. Actions returning no body
  (`NoContent()`/`NotFound()` only, e.g. `PUT`/`DELETE`) stay `IActionResult` — there's no `T`.
- **Declare every status an action can return with `[ProducesResponseType]`, including the success
  one.** Use the generic form: `[ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]`.
  This is not redundant with `ActionResult<T>`, and the reason is a real trap: adding *any*
  `[ProducesResponseType]` to an action **replaces** the framework's inferred 200 rather than
  adding to it. Annotating only the `404` silently drops the `200` and its schema from the OpenAPI
  document — the endpoint then documents its error and not its success. Being exhaustive on every
  action is what keeps that from happening.
- After changing any action's attributes or signature, **verify against the live
  `/openapi/v1.json`**, don't assume the attributes did what you expect — see
  `.claude/agents/docs-writer.md` and `.claude/skills/run-api/SKILL.md`. A `404` renders as a
  `ProblemDetails` body, which `[ApiController]` produces automatically.
- **No authentication/authorization scheme is configured yet**, despite `Program.cs` calling
  `UseAuthorization()` — every endpoint is currently open.
- `docs/api.md` documents the HTTP surface (routes, DTOs, status codes) and must be updated in
  the same pass as any controller change — see `.claude/agents/docs-writer.md`.

## Current state

`CustomersController.cs` and `ProductsController.cs` are the controllers today.
`CustomersController.cs` is the richer of the two and the reference pattern to copy.
