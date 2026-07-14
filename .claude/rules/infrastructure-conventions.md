---
paths:
  - "src/AHC.Sandbox.Infrastructure/**/*"
---

# Infrastructure-layer conventions

- Implement technical/external-system ports defined in `Application` — never contain business
  rules, and never depend on `Api`.
- Register new services in `DependencyInjection.cs`'s `AddInfrastructure()` — not in
  `Program.cs`.
- Treat any external dependency here (Redis, and anything added later) as something the API must
  survive without — a failure in this layer should degrade gracefully, not surface as an API
  outage.

## Current state — Redis caching

`RedisCustomerCacheRepository` implements `ICustomerCacheRepository` (the port, owned by
`Application/Customers/Interfaces/`) for the `Customer` resource: cache-aside on
`CustomerService.GetCustomerByIdAsync`, invalidate-on-write on `UpdateCustomerAsync`/
`PatchCustomerAsync`/`DeleteCustomerAsync`, `customer:{id}` keys, 5-minute absolute TTL.

`RedisOptions.ConnectTimeoutMs` (bound from the `Redis` appsettings section) governs both connect
and per-command (`SyncTimeout`/`AsyncTimeout`) timeouts — kept short (1 second) deliberately,
since it directly bounds how much latency a Redis outage adds to a request before
`CustomerService` falls back to the database. `AbortOnConnectFail = false` on the
`IConnectionMultiplexer` registration means the app still starts if Redis is unreachable at boot.

`RedisCustomerCacheRepository` catches `StackExchange.Redis.RedisException` and translates it into
`CacheUnavailableException` (also owned by `Application`) — this keeps the concrete Redis
exception type from leaking into `Application`, while still letting `CustomerService` distinguish
"the cache backend is down" (fall back to the database) from a genuine bug in cache code (e.g. a
JSON deserialization failure), which is deliberately left unwrapped so it surfaces rather than
being mistaken for an outage.

See `.claude/agents/redis-cache-builder.md` for the cache-aside/invalidate-on-write pattern this
follows.

## Reference

`Caching/RedisCustomerCacheRepository.cs` and `DependencyInjection.cs`'s `AddInfrastructure`
are the reference implementation for this layer.
