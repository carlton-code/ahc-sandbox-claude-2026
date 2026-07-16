---
paths:
  - "src/AHC.Sandbox.Domain/**/*"
---

# Domain-layer conventions

- **Zero dependencies.** This project must not reference any other project in the solution
  (`Application`, `Data`, `Infrastructure`, `Api`) — it's the innermost ring; everything else
  depends on it, never the reverse.
- Depend only on .NET base class libraries and other internal `Domain` types.
- No frameworks, no I/O, no persistence/HTTP concerns here — see
  `ReadMe-Domain.md` for the full contract of what does/doesn't belong.

## Current state

`Customer`, `Address`, and `Product` (under `Entities/`) are the only entities today, and all are
intentionally simple — plain properties plus one computed property each (`FullName`,
`SingleLineAddress`, `IsDiscontinued`). There's no other business logic here yet because nothing
built so far has needed it.

As soon as a real business rule shows up (rewards-tier eligibility, order/bundle validation,
etc.), it belongs on the relevant domain entity or a domain service — not bolted onto an
`Application` service or a controller. See
`.claude/skills/clean-architecture/SKILL.md`'s note on anemic-model drift for why this matters
more than it looks like it does right now.

## Reference

`Entities/Customer.cs` and `Entities/Address.cs` are the current examples of everything this
layer should look like: plain properties, one computed property, zero framework attributes or
dependencies.
