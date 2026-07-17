# AHC.Sandbox.Domain

## Purpose

The `AHC.Sandbox.Domain` project contains the core business model for the solution. It represents
the business concepts, rules, and behaviors that are central to the application regardless of how
it's accessed — no framework, no database, no HTTP.

## Responsibilities

The Domain layer is responsible for:

- Defining core business entities (currently `Customer`, `Address`, `Product`, `Order`, and
  `OrderLine`, under `Entities/`)
- Defining domain rules and invariants as they emerge
- Defining value objects, enums, and domain events, if/when the model needs them

**Current state:** `Customer`, `Address`, `Product`, `Order`, and `OrderLine` are the only
entities, and all are intentionally simple today — plain properties plus one computed property
on most (`FullName`, `SingleLineAddress`, `IsDiscontinued`, `IsShipped`). `Order` is the first
aggregate-shaped entity, carrying an `IReadOnlyCollection<OrderLine>`. There's no other business
logic here yet because none of
the resources implemented so far have needed it. As soon as a real
business rule shows up (e.g. rewards-tier eligibility, order/bundle validation), it belongs on the
relevant domain entity or a domain service — not bolted onto an `Application` service or, worse, a
controller. See `.claude/skills/clean-architecture/SKILL.md`'s note on anemic-model drift.

## Design Principles

The Domain layer should:

- Be independent of frameworks and delivery mechanisms
- Have zero dependencies on databases, EF Core, ASP.NET Core, or any other project in this
  solution
- Focus on business meaning and correctness, not on how a value is stored or transmitted
- Stay reusable regardless of what hosts consume it in the future

## Dependencies

This project should depend only on:

- .NET base class libraries
- other internal `Domain` types

This project must **not** reference any other project in this solution (`Application`, `Data`,
`Infrastructure`, `Api`) — it's the innermost ring; everything else depends on it, never the
reverse.

## Examples

Code that belongs here:

- Entity definitions (`Customer`, `Address`, `Product`, `Order`/`OrderLine` today; future
  entities for bundles, rewards tiers, etc. as those resources are built out)
- Value object definitions, business rules, and guard clauses, once they exist
- Domain services with pure business behavior (no persistence, no I/O)

Code that does **not** belong here:

- SQL queries or EF Core configuration
- API controllers or HTTP request/response models
- Logging, file system access, or any other I/O

## Goal

Preserve the business model in a clean, framework-independent way so the rest of the application
can build on a stable foundation. See `.claude/skills/clean-architecture/SKILL.md` for how this
project fits into the solution's overall dependency rule.
