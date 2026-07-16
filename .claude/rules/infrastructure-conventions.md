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

Caching exists for two resources, one repository per Application-owned port:
`RedisCustomerCacheRepository` (`ICustomerCacheRepository` — cache-aside on
`CustomerService.GetCustomerByIdAsync`, invalidate-on-write on `UpdateCustomerAsync`/
`PatchCustomerAsync`/`DeleteCustomerAsync`, `customer:{id}` keys) and
`RedisProductCacheRepository` (`IProductCacheRepository` — the same pattern on `ProductService`,
`product:{id}` keys). Both use a 5-minute absolute TTL. The contracts stay per-resource
deliberately (see `.claude/agents/redis-cache-builder.md` step 6) — don't fold them into a
generic `ICacheRepository<T>` without a third consumer forcing the question.

`RedisOptions.ConnectTimeoutMs` (bound from the `Redis` appsettings section) governs both connect
and per-command (`SyncTimeout`/`AsyncTimeout`) timeouts — kept short (1 second) deliberately,
since it directly bounds how much latency a Redis outage adds to a request before
`CustomerService` falls back to the database. `AbortOnConnectFail = false` on the
`IConnectionMultiplexer` registration means the app still starts if Redis is unreachable at boot.

Both repositories catch `StackExchange.Redis.RedisException` and translate it into
`CacheUnavailableException` (shared across resources, owned by `Application/Caching/`) — this
keeps the concrete Redis exception type from leaking into `Application`, while still letting the
consuming service distinguish "the cache backend is down" (fall back to the database) from a
genuine bug in cache code (e.g. a JSON deserialization failure), which is deliberately left
unwrapped so it surfaces rather than being mistaken for an outage.

See `.claude/agents/redis-cache-builder.md` for the cache-aside/invalidate-on-write pattern this
follows.

## Reference

`Caching/RedisCustomerCacheRepository.cs` and `DependencyInjection.cs`'s `AddInfrastructure`
are the reference implementation for this layer.
