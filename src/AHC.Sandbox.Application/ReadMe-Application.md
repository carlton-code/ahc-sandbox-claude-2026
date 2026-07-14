# AHC.Sandbox.Application

## Purpose

The `AHC.Sandbox.Application` project contains the use-case logic that coordinates workflows on
top of the Domain layer. It's consumed by `AHC.Sandbox.Api` — the only host in this solution —
but is written without any knowledge of HTTP, so nothing stops another host from reusing it later.

## Responsibilities

The Application layer is responsible for:

- Implementing use cases (one service per resource, e.g. `CustomerService`)
- Coordinating domain objects and the repository ports that read/write them
- Defining DTOs — the boundary shape between the outside world and the domain (e.g.
  `CustomerDto`, `CreateCustomerDto`, `UpdateCustomerDto`, `PatchCustomerDto`,
  `CustomerSummaryDto`, `CustomerOrderDto`, `CustomerOrderSummaryDto`)
- Defining the repository and service **interfaces** that `Data`/`Infrastructure` implement
  (e.g. `ICustomerReadRepository`, `ICustomerWriteRepository`, `ICustomerService`) — Application
  owns these ports, it doesn't implement them
- Mapping between `Domain` entities and DTOs

Code is organized per resource under `<Resource>/Dtos`, `<Resource>/Interfaces`,
`<Resource>/Services` (see the `Customers/` folder) — follow that same shape for a new resource
rather than inventing a different one.

## Design Principles

The Application layer should:

- Depend on `Domain` only
- Express use cases clearly, one service method per use case
- Avoid HTTP-specific concerns (no `IActionResult`, no status codes — that's `Api`'s job)
- Depend on abstractions (its own interfaces) rather than concrete `Data`/`Infrastructure` types
- Split read and write concerns into separate repository interfaces per resource
  (`I<Resource>ReadRepository` / `I<Resource>WriteRepository`) rather than one combined interface

## Dependencies

This project references:

- `AHC.Sandbox.Domain`

This project must **not** reference `AHC.Sandbox.Data`, `AHC.Sandbox.Infrastructure`, or
`AHC.Sandbox.Api` — those layers depend on Application, not the other way around. All
dependencies on persistence or external systems flow through interfaces defined here.

## Examples

Code that belongs here:

- Service implementations — `CustomerService`
- Service and repository interfaces — `ICustomerService`, `ICustomerReadRepository`,
  `ICustomerWriteRepository`
- DTOs and Domain ↔ DTO mapping
- `DependencyInjection.cs`'s `AddApplication()` extension method, registering the service(s)

Code that does **not** belong here:

- Controllers or anything HTTP-shaped
- `DbContext` or EF Core types
- Raw SQL
- Redis, SMTP, or other external-system implementations

## Goal

Provide a clean application boundary that keeps use-case logic independent of both the delivery
mechanism (`Api`) and the technical implementations (`Data`, `Infrastructure`) that back it. See
`.claude/skills/clean-architecture/SKILL.md` for the reasoning, and
`.claude/agents/api-scaffolder.md` for scaffolding a new resource's Application-layer pieces.
