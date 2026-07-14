# ADR-0004: Redis over `IMemoryCache` for caching

## Status

Accepted

## Context

`CustomerService.GetCustomerByIdAsync` is a good candidate for caching — single-row, read-heavy,
rarely-changing. .NET offers `IMemoryCache` (in-process, zero extra infrastructure, but scoped to
one instance and lost on restart) and distributed options like Redis (an extra moving part to run
and operate, but shared across instances and survives an app restart).

## Decision

Use Redis (via `StackExchange.Redis`), configured through `RedisOptions` and an
`ICustomerCacheRepository` port (owned by `Application`, implemented by `Infrastructure`), rather
than `IMemoryCache`. Two reasons:

1. This is explicitly a hands-on learning exercise for Redis/distributed caching concepts, not
   just the fastest way to solve today's caching need — see `.claude/agents/redis-cache-builder.md`.
2. Redis caching survives an API restart and would behave correctly if this API ever ran as more
   than one instance; `IMemoryCache` fundamentally can't do either — each instance would keep its
   own separate, inconsistent cache.

## Consequences

- An operational dependency is added (a Redis instance must be reachable, locally or otherwise)
  for a feature `IMemoryCache` could have solved with zero extra infrastructure — an accepted
  cost given the learning goal and the multi-instance argument above.
- Redis must be treated as an optimization the API can survive without: a Redis outage should
  degrade to a database read, not surface as an API failure. This is called out explicitly in
  `.claude/agents/redis-cache-builder.md` as the most important production lesson this exercise
  is meant to teach.
- `ICustomerCacheRepository` lives in `Application` (`Customers/Interfaces/`), not `Infrastructure`,
  so `CustomerService` depends on the port without depending on the concrete Redis implementation.
  `RedisCustomerCacheRepository` (`Infrastructure`) translates `StackExchange.Redis.RedisException`
  into an `Application`-owned `CacheUnavailableException` at that boundary, so `CustomerService`
  can fall back to the database on a genuine cache outage without depending on
  `StackExchange.Redis` itself.
