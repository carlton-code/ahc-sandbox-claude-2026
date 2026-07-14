# ADR-0006: DTOs as the API boundary, never entities

## Status

Accepted

## Context

`Api` controllers need a shape to bind incoming requests to and return in responses. Both the
domain model (`Customer`) and the EF entity (`CustomerEntity`) already closely resemble what a
client would send/receive, which makes binding directly to one of them tempting — it would mean
one fewer class to maintain per resource.

## Decision

Controllers bind exclusively to dedicated DTOs (`CustomerDto`, `CreateCustomerDto`,
`UpdateCustomerDto`, `PatchCustomerDto`, ...) — never to `CustomerEntity` or `Customer` directly,
even for resources where the DTO ends up looking like a near-duplicate of the entity it's derived
from.

## Consequences

- **Mass-assignment/over-posting safety.** A DTO only exposes the fields a client should
  legitimately be able to set. At the time this decision was made, `SalesLT.Customer` still had
  `PasswordHash`/`PasswordSalt` columns (since dropped — see ADR-0007) that never appeared in any
  DTO, so there was no path — accidental or otherwise — for a client to set them. The general
  principle holds regardless of which specific columns a table happens to have.
- **`Data` is free to evolve independently.** An EF entity can gain a column, change a mapped
  length, or be restructured without silently changing the API's public contract, because the
  contract is the DTO, not the entity.
- **Cost:** near-duplicate classes for simple resources — a DTO whose properties are nearly
  identical to its entity's. This is a deliberate, accepted cost of the safety/decoupling benefit
  above, not an oversight to "simplify away" by collapsing DTO and entity into one class. See
  `.claude/agents/code-reviewer.md`'s API/DTO security hygiene checklist, which checks for exactly
  this.
