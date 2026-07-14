---
paths:
  - "src/AHC.Sandbox.Application/**/*"
---

# Application-layer conventions

- Depend on `Domain` only. Never reference `Data`, `Infrastructure`, or `Api` concretely — this
  layer **owns** the ports (interfaces) that those layers implement, it doesn't implement them.
- Organize code per resource, mirroring `Customers/`: `<Resource>/Dtos/`,
  `<Resource>/Interfaces/`, `<Resource>/Services/`. Don't invent a different shape for a new
  resource.
- **Split read and write repository interfaces** per resource —
  `I<Resource>ReadRepository` / `I<Resource>WriteRepository` — rather than one combined
  interface. See `docs/adr/` if a rationale is ever recorded for this; today it's an established
  convention to keep consistent, not a one-off.
- DTOs are the boundary shape crossing into/out of this layer — map `Domain` ↔ DTO here.
  Controllers in `Api` bind to these DTOs, never to a `Domain` entity directly.
- Register new services in `DependencyInjection.cs`'s `AddApplication()` — not in `Program.cs`.
- **Don't over-scaffold DTOs.** A new resource shouldn't automatically get an
  `Update<Resource>Dto`/`Patch<Resource>Dto`/`<Resource>SummaryDto` just because `Customer` has
  them — only add what that resource's actual use cases need. See
  `.claude/agents/api-scaffolder.md`.

## Reference

`Customers/Dtos/*`, `Customers/Interfaces/*`, and `Customers/Services/CustomerService.cs` are the
current example of this layer's shape — `CustomerService` orchestrates
`ICustomerReadRepository`/`ICustomerWriteRepository` and maps `Domain.Customer` → `CustomerDto`.

See `ReadMe-Application.md` for the full layer contract and
`.claude/skills/clean-architecture/SKILL.md` for why the dependency direction matters.
