---
name: nuget-package-audit
description: Use for periodic NuGet dependency maintenance on AHC.Sandbox — checks every project for outdated and vulnerable packages via `dotnet list package`, then applies this project's policy on when to bump immediately, bump routinely, bump-with-verification, or hold. Not for adding a brand-new package to the solution (that's a normal `dotnet add package`).
---

# NuGet package audit

A repeatable walkthrough for checking this solution's dependencies and deciding what to do about
what you find — not just running the commands, but applying a consistent policy so "should I
bump this" isn't re-litigated from scratch every time.

## 1. Check for outdated packages

From the repo root (auto-discovers `AHC.Sandbox.slnx`):

```
dotnet list package --outdated
```

This reports every project's direct package references with their current, resolved, and latest
available versions. Add `--include-transitive` if you also want to see outdated transitive
dependencies, not just top-level ones.

## 2. Check for known vulnerabilities

```
dotnet list package --vulnerable --include-transitive
```

**Always include `--include-transitive` here** — most real-world advisories land in a transitive
dependency (a package your packages depend on), not a top-level one, and the non-transitive form
alone will miss those.

## 3. Apply the bump-vs-hold policy

### Always bump immediately, no exceptions

Anything `--vulnerable` flags, at any severity, regardless of how large the version jump is.
Security fixes aren't optional. After bumping, re-run `dotnet list package --vulnerable
--include-transitive` to confirm the advisory is actually resolved (a transitive vulnerability
sometimes needs a direct reference added/bumped to force resolution, not just a wait-and-hope).

### Bump routinely, low ceremony

- **Patch version bumps** (`x.y.Z`) on any package — bug-fix-only by semver contract, minimal
  risk.
- **Minor version bumps** (`x.Y.z`) on test-only tooling: `NUnit`, `NUnit3TestAdapter`,
  `NUnit.Analyzers`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`. These don't ship to
  production, so the blast radius of a regression is much smaller.

Still run `dotnet build` after — don't skip verification just because ceremony is low.

### Bump, but verify before calling it done

**Minor version bumps** (`x.Y.z`) on anything that actually ships — `Microsoft.EntityFrameworkCore*`,
`Microsoft.AspNetCore.OpenApi`, `Microsoft.OpenApi`, `Swashbuckle.AspNetCore.SwaggerUI`, and
(once it exists) `StackExchange.Redis`. Bump, then `dotnet build` + `dotnet test` (both
`AHC.Sandbox.UnitTests` and `AHC.Sandbox.IntegrationTests`), and a quick smoke-check of the
affected behavior before treating it as done.

### Hold — needs a deliberate look, not a reflexive bump

- **Any major version bump** (`X.y.z`), on any package. Semver permits breaking changes here.
  Read the release notes / breaking-changes doc first; don't bump just because `--outdated` shows
  a newer major exists. If you decide to adopt it, that's often worth its own entry in
  `docs/adr/` (see `docs/adr/template.md`) rather than a silent version-number change, especially
  for something as central as EF Core.
- **Prerelease/preview versions.** This solution targets `net10.0` as a stable release, not
  preview — don't pull in a `-preview`/`-rc` package version unless there's a specific, named
  reason tied to a feature this project actually needs.

### Version-lockstep groups — bump together or not at all

Some packages in this solution must move in sync; bumping one without the others is a common,
real source of runtime errors, not just a style preference:

- **`Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.SqlServer`** (both in
  `AHC.Sandbox.Data.csproj`, currently pinned at the same version) must stay on the exact same
  version — the SQL Server provider is versioned in lockstep with the EF Core meta-package, and
  mismatches can fail at runtime rather than at build time.
- **`NUnit`, `NUnit3TestAdapter`, `NUnit.Analyzers`** should be bumped as a set — check they're
  still mutually compatible rather than assuming "latest of each" always is.
- **`AHC.Sandbox.UnitTests.csproj` and `AHC.Sandbox.IntegrationTests.csproj`** carry the identical
  NUnit/test-tooling package set by design (see `docs/adr/0005-split-unit-and-integration-test-projects.md`
  and `.claude/rules/testing-conventions.md`). A version bump to one must be mirrored in the
  other in the same pass — don't let the two drift.

## 4. Wrap up

- Re-run `dotnet list package --outdated` and `--vulnerable --include-transitive` once more to
  confirm the audit's findings are actually resolved.
- Run `dotnet build` and `dotnet test` (or delegate to `.claude/agents/test-runner.md`) before
  considering the audit done.
- If a bump was deliberate and architecturally significant (a major version, a new package, a
  policy exception), consider whether it belongs in `docs/adr/` — routine patch/minor bumps don't
  need one.
