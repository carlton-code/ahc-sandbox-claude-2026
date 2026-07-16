# AHC.Sandbox.UnitTests

## Purpose

The `AHC.Sandbox.UnitTests` project contains the fast, isolated tests for the solution — the
"unit" half of the unit/integration split. It verifies `Domain` invariants and `Application`
use-case logic without touching a real database, Redis, or the network. For anything that needs
real infrastructure, see the sibling `AHC.Sandbox.IntegrationTests` project instead.

**Current state:** real coverage exists — `Customers/CustomerServiceTests.cs` exercises
`CustomerService` (including its cache-aside/invalidation behavior) against hand-written fakes under
`Customers/Fakes/` (`FakeCustomerReadRepository`, `FakeCustomerWriteRepository`,
`FakeCustomerCacheRepository`), `Addresses/AddressServiceTests.cs` covers `AddressService`'s
not-found/empty-list semantics against `Addresses/Fakes/FakeAddressReadRepository`,
`Products/ProductServiceTests.cs` covers `ProductService`'s mapping, write pass-through, and
cache-aside/invalidation behavior against `Products/Fakes/`, and
`Domain/CustomerTests.cs` / `Domain/AddressTests.cs` /
`Domain/ProductTests.cs` cover the `Domain` computed properties. Adding more coverage as
resources get built out is expected, not optional.

## Responsibilities

This project is responsible for:

- Verifying business rules as they appear in `Domain`
- Testing `Application` service use cases (`CustomerService` and future services) against fake
  implementations of their repository interfaces
- Preventing regressions during refactoring and enhancement, fast enough to run on every save

## What belongs here vs. `AHC.Sandbox.IntegrationTests`

| | `UnitTests` | `IntegrationTests` |
|---|---|---|
| Speed | Milliseconds, no I/O | Seconds, hits real infrastructure |
| Dependencies under test | `Domain`, `Application` | `Data` (real SQL Server), `Infrastructure` (real Redis), `Api` end-to-end |
| Repository access | Fake/in-memory implementations of `I<Resource>ReadRepository`/`I<Resource>WriteRepository` | The real `Data`/`Infrastructure` implementations |
| Run cadence | Every build, every save | Before a commit / in CI, when real infrastructure is available |

If a test needs a real `AdventureWorksLtDbContext`, a real Redis connection, or a running API
host, it belongs in `AHC.Sandbox.IntegrationTests`, not here.

## Design Principles

- Be fast and fully isolated — no real database, no real Redis, no network calls
- Cover critical business behavior and edge cases as they're implemented
- Make failures easy to understand and diagnose

### Conventions actually in use

- **NUnit** (not xUnit/MSTest) — `[SetUp]` for per-test setup, `[Test]` on test methods,
  `Assert.That(actual, Is.EqualTo(expected))` constraint-model assertions. `using NUnit.Framework`
  is implicit via a global `<Using>` in the csproj.
- **No mocking library is referenced** (no Moq, NSubstitute, or FluentAssertions in the csproj) —
  hand-write a minimal fake implementation of a repository interface rather than reaching for a
  new package, or raise it with the team first if a real mocking library is genuinely needed.
- Name test classes/files after the unit under test (e.g. `CustomerServiceTests`), not
  generically like `UnitTest1`.

## Dependencies

This project references every `src` project (so it can test any layer's in-process logic without
real I/O):

- `AHC.Sandbox.Domain`
- `AHC.Sandbox.Application`
- `AHC.Sandbox.Data`
- `AHC.Sandbox.Infrastructure`
- `AHC.Sandbox.Api`

Plus NUnit, `NUnit3TestAdapter`, `NUnit.Analyzers`, `Microsoft.NET.Test.Sdk`, and
`coverlet.collector`.

## Examples

Code that belongs here:

- Domain invariant tests, once `Domain` has real invariants to test
- `CustomerService`-style application-service tests against fake repositories

## Goal

Give fast, always-runnable feedback on business and use-case logic, leaving anything that needs
real infrastructure to `AHC.Sandbox.IntegrationTests`. See `.claude/agents/test-runner.md` for
building, running, and writing tests in this project's existing style, and
`.claude/commands/verify.md` for the build-test-review pipeline to run before committing
non-trivial changes.
