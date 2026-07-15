# AHC.Sandbox.Infrastructure

## Purpose

The `AHC.Sandbox.Infrastructure` project contains implementations of technical services that
aren't the primary database — external systems and framework-based services that support the
Application layer without the Application layer knowing the concrete details.

## Responsibilities

The Infrastructure layer is responsible for:

- Implementing technical service interfaces defined in `Application`
- Currently: **Redis caching** for the `Customer` resource, under `Caching/` and `Configuration/`

**Current state:**

- `Configuration/RedisOptions.cs` — bound from the `Redis` appsettings section
  (`Configuration`, `UseTls`, `ConnectTimeoutMs`). `ConnectTimeoutMs` governs both the initial
  connect timeout and per-command (`SyncTimeout`/`AsyncTimeout`) — it's what bounds how much
  latency a Redis outage adds to a request before falling back to the database.
- `Caching/RedisCustomerCacheRepository.cs` implements `ICustomerCacheRepository` (the port,
  defined in `Application/Customers/Interfaces/`) using `StackExchange.Redis`, with `customer:{id}`
  keys and a 5-minute absolute TTL. It catches `StackExchange.Redis.RedisException` at this
  boundary and translates it into `CacheUnavailableException` (also defined in `Application`) —
  keeping the concrete Redis exception type out of `Application` entirely, while still letting
  `CustomerService` distinguish "the cache backend is unreachable" from a genuine bug (e.g. a
  JSON deserialization failure, which is deliberately left unwrapped and propagates normally).
- `DependencyInjection.cs`'s `AddInfrastructure(IConfiguration)` registers `IConnectionMultiplexer`
  as a singleton (`AbortOnConnectFail = false`, so the app still starts if Redis is unreachable at
  boot) and `ICustomerCacheRepository` as scoped.

See `.claude/agents/redis-cache-builder.md` for the cache-aside/invalidate-on-write pattern this
follows, and `docs/adr/0004-redis-for-caching-over-in-memory.md` for the reasoning behind
choosing Redis over `IMemoryCache`.

## Design Principles

The Infrastructure layer should:

- Implement interfaces defined in `Application` — if the interface a new service needs doesn't
  exist yet, define it in `Application`, not in this project
- Isolate external dependencies (Redis, and anything else added later) from the rest of the
  application
- Be replaceable/mockable for testing — `Application` should depend on the interface, never on
  `RedisCustomerCacheRepository` or `IConnectionMultiplexer` directly
- Treat caching as an optimization the API can survive without — a Redis outage should degrade to
  a database read, not surface as an API failure
- Avoid containing business rules that belong in `Domain` or `Application`

## Dependencies

This project references:

- `AHC.Sandbox.Application`
- `AHC.Sandbox.Domain`

It also references `StackExchange.Redis`.

## Examples

Code that belongs here:

- `RedisCustomerCacheRepository` (implementing the `ICustomerCacheRepository` port defined in
  `Application`)
- `IConnectionMultiplexer` registration and connection configuration
- Any future non-database external integration (email, file storage, etc.), following the same
  "implement an Application-owned port" shape

Code that does **not** belong here:

- Business entities or use-case orchestration
- Database schema definitions or EF Core mappings (those are `Data`)
- API endpoints

## Goal

Isolate technical implementation details — starting with Redis caching — so the rest of the
solution depends on a clean abstraction rather than a concrete external dependency. See
`.claude/skills/clean-architecture/SKILL.md` for why that separation exists.
