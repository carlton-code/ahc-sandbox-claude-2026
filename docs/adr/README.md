# Architecture Decision Records

This folder holds short, one-decision-per-file records of the significant architectural choices
behind AHC.Sandbox — not just *what* was decided, but the alternatives considered and *why* one
was picked over the others. They're deliberately brief: an ADR that takes ten minutes to read
doesn't get read.

These double as context for Claude Code (and any future contributor): read the relevant ADR
before proposing to change a decision it covers, so the original trade-off gets weighed again
rather than silently reversed.

## Format

Each ADR follows the same shape (see `template.md`):

- **Status** — `Accepted`, `Superseded by ADR-000X`, etc.
- **Context** — the situation and the realistic alternatives at the time.
- **Decision** — what was actually chosen.
- **Consequences** — the trade-offs accepted as a result, good and bad.

## Index

| ADR | Decision |
|---|---|
| [0001](0001-clean-architecture-over-vertical-slices.md) | Clean/layered architecture over vertical slices |
| [0002](0002-ef-core-over-dapper.md) | EF Core + raw ADO.NET over adding Dapper |
| [0003](0003-nunit-over-xunit-mstest.md) | NUnit over xUnit/MSTest |
| [0004](0004-redis-for-caching-over-in-memory.md) | Redis over `IMemoryCache` for caching |
| [0005](0005-split-unit-and-integration-test-projects.md) | Split test projects into `UnitTests` and `IntegrationTests` |
| [0006](0006-dtos-as-the-api-boundary.md) | DTOs as the API boundary, never entities |
| [0007](0007-drop-password-columns-from-customer.md) | Drop `PasswordHash`/`PasswordSalt` from `SalesLT.Customer` |
| [0008](0008-one-rewards-tier-per-customer.md) | Enforce one rewards tier per customer at the schema level |

## Adding a new ADR

1. Copy `template.md` to `NNNN-short-kebab-case-title.md`, using the next sequential number.
2. Fill in Status/Context/Decision/Consequences. Keep it short — a paragraph or two per section
   is usually enough.
3. Add a row to the index table above.
4. If the new ADR changes or reverses an earlier one, update that earlier ADR's Status to
   `Superseded by ADR-NNNN` rather than deleting it — the old reasoning is still useful history.
