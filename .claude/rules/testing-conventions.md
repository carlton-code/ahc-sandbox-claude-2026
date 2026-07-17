---
paths:
  - "tests/**/*"
---

# Testing conventions

Both test projects are **NUnit** (not xUnit/MSTest) — `[SetUp]` for per-test setup, `[Test]` on
test methods, `Assert.That(actual, Is.EqualTo(expected))` constraint-model assertions (or
`Assert.Pass()` only as a placeholder, never in a real test). `using NUnit.Framework` is implicit
via a global `<Using>` in each csproj — don't re-add it. See
`docs/adr/0003-nunit-over-xunit-mstest.md` for why, and don't mix in xUnit-style
`[Fact]`/`[Theory]` attributes out of habit.

**No mocking library is referenced** in either project (no Moq/NSubstitute/FluentAssertions) —
hand-write a minimal fake implementation of a repository interface, or raise it with the user
first if a real mocking library is genuinely needed.

Name test classes/files after the unit under test (e.g. `CustomerServiceTests`), not generically
like `UnitTest1`.

## Which project a new test goes in

See `docs/adr/0005-split-unit-and-integration-test-projects.md` for the full rationale.

- **`AHC.Sandbox.UnitTests`** — anything that can run against a fake with no real database, no
  real Redis, no network. This is almost everything: `Domain` invariants, `Application` service
  logic against fake repository interfaces (`ICustomerReadRepository`, etc.).
- **`AHC.Sandbox.IntegrationTests`** — anything that fundamentally needs the real thing:
  repository methods against a real SQL Server (real SQL translation and collation-dependent
  ordering, plus the one raw-ADO.NET method, `CustomerReadRepository.GetRewardsAsync`),
  `Infrastructure`'s Redis caching (`RedisCustomerCacheRepository`), or a full
  end-to-end `Api` test. Connection configuration is wired up via this project's own
  `appsettings.json` plus `Infrastructure/TestConfiguration.cs`, `DbContextTestFactory.cs`, and
  `RedisTestFixture.cs` — reuse those rather than adding a second way to read connection strings.

If in doubt, default to `UnitTests` with a fake — only reach for `IntegrationTests` when a fake
genuinely can't exercise what needs testing.

## Running tests

```
dotnet build
dotnet test
```

`dotnet test` runs both projects from the repo root; scope to one with
`dotnet test tests/AHC.Sandbox.UnitTests` or `tests/AHC.Sandbox.IntegrationTests`. See
`.claude/agents/test-runner.md` for diagnosing failures and writing new tests.
