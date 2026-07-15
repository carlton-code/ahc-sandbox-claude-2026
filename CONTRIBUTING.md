# Contributing to AHC.Sandbox

This is currently a solo/learning sandbox, not a multi-contributor project with a live PR queue —
but the conventions below are written to hold up if that changes, and they double as a
pre-merge self-review checklist even when there's no one else to review the diff. Follow them
now so they're already a habit if a remote and collaborators show up later.

## Branching model

`main` is **production**. `develop` is the integration branch and the base for everyday work.

| Branch | Cut from | Merges into |
|---|---|---|
| `feat/*`, `fix/*`, `docs/*`, `refactor/*`, `test/*`, `chore/*`, `adr/*` | `develop` | `develop` |
| `release/*` | `develop` | `main` **and** back into `develop` |
| `hotfix/*` | `main` | `main` **and** back into `develop` |

Two rules follow from that table, and they're the ones worth remembering:

- **Nothing is committed to `main` directly, and only `release/*` and `hotfix/*` ever merge into
  it.** Day-to-day branches never target `main` — they go to `develop` and reach production later,
  as part of a release.
- **Anything that lands on a `release/*` or `hotfix/*` branch merges back into `develop`.** A
  version bump, a bug caught during release testing, an urgent production fix — all of it reaches
  `main` through the release or hotfix branch, but `develop` only learns about it via the
  back-merge. Skip the back-merge and `develop` is silently missing a fix that's live in
  production: the next branch is cut from `develop`, and the following release quietly reintroduces
  the bug.

A `hotfix/*` is cut from `main`, not `develop`, precisely because `develop` may already carry
unreleased work that isn't ready to ship. That's the entire reason the branch type exists — if the
fix can wait for the next release, it's a `fix/*` off `develop` instead.

## Branch naming

`<type>/<short-kebab-description>`, matching the commit types below — plus `release/*` and
`hotfix/*`, which come from the branching model above rather than from a commit type:

```
feat/<resource>-crud
fix/customer-email-nullability
docs/api-md-drift
refactor/customer-service-mapping
test/customer-service-fakes
chore/nuget-audit-2026-07
adr/redis-vs-inmemory-cache
hotfix/customer-search-null-reference
```

- Lowercase, hyphen-separated, no ticket-number-only branch names — the description should say
  what the branch does without needing to look anything up.
- Name it after the resource/area touched when that's clearer than the change type alone (e.g.
  `feat/<resource>-crud` over `feat/new-endpoint`).
- `release/*` is the one exception to the description rule — it's named for the version it ships
  (`release/1.2.0`), since that *is* the description.

## Commit message format

[Conventional Commits](https://www.conventionalcommits.org/)-style, since this repo already
tracks *why* decisions were made in `docs/adr/` — commit messages should carry the same habit at
a smaller scale:

```
<type>(<scope>): <short summary, imperative mood>

<body — why this change, not what it does; the diff already shows what>

<optional footer — e.g. "See docs/adr/0004-redis-for-caching-over-in-memory.md">
```

**Types:** `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `build`.

**Scopes** — match the project/area touched:

| Scope | Area |
|---|---|
| `domain`, `application`, `data`, `infrastructure`, `api` | The matching `src/AHC.Sandbox.*` project |
| `tests` | Either test project (`UnitTests`/`IntegrationTests`) |
| `docs` | `docs/api.md`, `docs/database-schema.md`, or other non-ADR docs |
| `adr` | A new or superseded `docs/adr/` entry |
| `claude` | `.claude/agents`, `.claude/commands`, `.claude/skills`, `.claude/rules`, or `CLAUDE.md` |
| `deps` | Package version bumps (see `.claude/skills/nuget-package-audit/SKILL.md`) |

**Examples:**

```
feat(api): add <Resource> CRUD endpoints

Builds out <Resource>Controller following the Customer vertical slice pattern.

docs(docs): update docs/api.md for the new <Resource> endpoints
```

```
fix(data): guard against null EmailAddress on Customer read

SalesLT.Customer.EmailAddress is nullable in the database but CustomerEntity
declared it non-nullable, throwing on materialization for any row with a null
email. See docs/database-schema.md's known-gotcha note.
```

## Before opening a pull request (or merging to `develop` solo)

Everyday branches target `develop` — see the branching model above. The one checklist item that
changes for a `release/*` or `hotfix/*` branch is the last one.

- [ ] `dotnet build` succeeds with no new warnings.
- [ ] `dotnet test` passes — both `AHC.Sandbox.UnitTests` and `AHC.Sandbox.IntegrationTests` (or
      note explicitly if integration tests were skipped because real infrastructure wasn't
      available locally).
- [ ] Ran `.claude/commands/verify.md` (`/verify`) — build, test, and a review pass against the
      architecture/SQL-safety checklists.
- [ ] If a controller changed: `docs/api.md` is updated to match (see
      `.claude/agents/docs-writer.md` — verify against the live OpenAPI document, don't just
      eyeball the controller).
- [ ] If a new table got wired into `Data`: `docs/database-schema.md` moved it from "not in use"
      to "actually in use" (see `.claude/agents/api-scaffolder.md`).
- [ ] If this change makes or reverses a significant architectural decision: a new ADR was added
      under `docs/adr/` (or an existing one marked `Superseded by ADR-NNNN`), not just a silent
      reversal.
- [ ] Layering rules respected — dependency direction, DI registered in the owning layer's
      `Add<Layer>()` extension method, controllers stay thin. Worth a pass from
      `.claude/agents/architecture-reviewer.md` if the change spans multiple projects.
- [ ] Raw SQL touched? Reviewed against `.claude/agents/sql-safety-reviewer.md` (parameterization,
      schema correctness, connection lifecycle).
- [ ] A general correctness/quality pass from `.claude/agents/code-reviewer.md` (or Claude Code's
      built-in `/code-review`) for anything non-trivial.
- [ ] No real credentials/connection strings committed — `appsettings.Development.json` should
      only ever point at a local/dev database.
- [ ] Branch name and commit messages follow the conventions above.
- [ ] **Merging a `release/*` or `hotfix/*` into `main`?** It merges back into `develop` too. The
      work isn't done until both merges have landed — a shipped release branch that never made it
      back to `develop` is the failure mode the branching model exists to prevent.

## Where to start

New here? Read `README.md` first for the project tour, then `CLAUDE.md` and the matching
`.claude/rules/*.md` file for whatever project you're about to touch — see the table in
`CLAUDE.md` for which rule file applies where.
