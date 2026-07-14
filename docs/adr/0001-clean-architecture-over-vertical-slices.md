# ADR-0001: Clean/layered architecture over vertical slices

## Status

Accepted

## Context

Two mainstream ways to structure a .NET API of this shape:

- **Clean/layered architecture**: separate projects per ring (`Domain`, `Application`, `Data`,
  `Infrastructure`, `Api`), with the dependency rule enforced by the project reference graph
  itself. A single resource's code is spread across four projects.
- **Vertical slices**: one folder (often one project) per feature/use case, with everything a
  given request handler needs — request, handler, query, response — colocated. Optimizes for
  feature cohesion over strict layer separation.

Both are legitimate, widely-used choices; neither is objectively "more correct." The right one
depends on what the project is optimizing for.

## Decision

Use a layered Clean Architecture: `AHC.Sandbox.Domain` → `AHC.Sandbox.Application` →
`AHC.Sandbox.Data`/`AHC.Sandbox.Infrastructure` → `AHC.Sandbox.Api`, with the dependency direction
enforced at compile time via project references (see `.claude/skills/clean-architecture/SKILL.md`
for the full mapping).

This project exists partly to practice the pattern itself — the README describes it as "used to
experiment with a clean, layered API architecture" — so a structure that makes each layer
boundary a separate, compiler-enforced project is more valuable here than one that would blur
those boundaries for the sake of feature cohesion. A vertical-slice structure would make it easy
to lose track of which layer a given piece of code actually belongs to, which is the opposite of
what this project is for.

## Consequences

- Adding a new resource touches four projects instead of one folder — more ceremony per resource
  than a vertical slice would need. `.claude/agents/api-scaffolder.md` exists specifically to
  absorb that ceremony.
- Navigating from a controller to the entity it ultimately reads from means jumping between
  projects, not just files.
- In exchange, the dependency rule is enforced by the compiler, not just convention — `Domain`
  physically cannot reference `Data`, because there's no project reference to do it with.
- If this project ever grows to many independent feature areas with little shared domain model,
  revisit whether vertical slices (or a hybrid) would serve better — this decision fits a
  single-domain sandbox, not necessarily a large multi-team system.
