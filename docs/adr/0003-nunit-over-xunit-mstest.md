# ADR-0003: NUnit over xUnit/MSTest

## Status

Accepted

## Context

.NET has three mainstream test frameworks — MSTest, NUnit, and xUnit — all functionally
comparable for this project's needs (attribute-based test discovery, assertion styles,
parallelizable runs). None of them offers a capability this project specifically needs that the
others lack.

## Decision

Use NUnit (`[Test]`/`[SetUp]`, `Assert.That(actual, Is.EqualTo(expected))` constraint-model
assertions) across both `AHC.Sandbox.UnitTests` and `AHC.Sandbox.IntegrationTests`. This was the
framework already in place when the test project was first scaffolded — there's no strong
technical reason it beats xUnit or MSTest at this project's scale. The value of documenting it
here isn't the choice itself, it's making sure nobody "helpfully" introduces xUnit conventions
(`[Fact]`/`[Theory]`) into a project that's standardized on NUnit's out of habit.

## Consequences

- All new tests, in either test project, use NUnit's attributes and assertion style — no mixing
  frameworks.
- Switching frameworks later is possible but not worth doing without a concrete reason (e.g. a
  capability NUnit is missing) — consistency matters more here than which framework it is.
