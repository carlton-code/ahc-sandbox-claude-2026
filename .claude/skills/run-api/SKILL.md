---
name: run-api
description: How to start, verify, and cleanly stop AHC.Sandbox.Api on this Windows/Git Bash setup for manual/curl-based testing — including the exact way to find and kill the right process, since the shell's own PID does not match the Windows process holding the port/build lock. Use this whenever you need to run the app locally (verifying an endpoint, checking OpenAPI drift, testing a resilience/fallback path) instead of hand-rolling `dotnet run` and `taskkill` from scratch.
---

# Running AHC.Sandbox.Api locally

## Starting it

Plain HTTP is easier for `curl`-based verification (no self-signed cert to work around):

```bash
cd <repo root>
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/AHC.Sandbox.Api --no-launch-profile --urls "http://localhost:5297" > /tmp/api.log 2>&1 &
```

If you specifically need the app's real launch profile (HTTPS on `7143`, matching
`Properties/launchSettings.json`), use `--launch-profile AHC.Sandbox.Api` instead of
`--no-launch-profile --urls ...`, and add `-k` to every `curl` call for the self-signed dev cert.

Wait for it to actually be up rather than assuming — poll instead of a fixed sleep:

```bash
for i in $(seq 1 15); do
  curl -s -o /dev/null -w "%{http_code}" http://localhost:5297/openapi/v1.json | grep -q 200 && break
  sleep 1
done
```

The app doesn't need a reachable SQL Server just to start and serve `/openapi/v1.json` — EF Core
doesn't connect until a query actually runs — so this works even without a database available.

## Before you start it: check what's already running

More than one local dependency may already be up from earlier work — don't assume a clean slate
and don't start a duplicate:

- `docker ps` — a Redis container (e.g. `local-redis`) may already be running on `6379`. Reuse it;
  don't `docker run` a second one on the same port (it'll fail with a port-already-allocated
  error).
- A local SQL Server may already be running as a Windows service, not a container — check with
  `powershell.exe -NoProfile -Command "Get-Service | Where-Object { \$_.Name -like '*SQL*' }"` if
  a connection failure is confusing rather than assuming a container is needed.

## Stopping it — the part that isn't obvious

`dotnet run` forks into a child process. The PID this shell backgrounds it under
(`$!` in Git Bash, or the PID `ps aux` shows) is **not** the Windows PID actually holding the port
and the build-output file locks — killing that one does nothing useful. Find the real one via
PowerShell, matching on command line, not by name alone:

```bash
powershell.exe -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Select-Object ProcessId, CommandLine"
```

Look for the row whose `CommandLine` contains `run --project src/AHC.Sandbox.Api` (there will
also be unrelated `dotnet.exe` rows for MSBuild build-server nodes — leave those running, they're
shared infrastructure, not something this session started). Kill only that specific PID:

```bash
taskkill //F //PID <that PID>
```

**Never** `taskkill //F //IM dotnet.exe` (or any other all-`dotnet.exe` wildcard) — that kills
every dotnet process on the machine, including unrelated MSBuild nodes and anything else running.
It will also be blocked by the auto-mode permission classifier for exactly this reason.

## Why this matters for rebuilding

If you forget to stop a running instance before `dotnet build`, the build fails with `MSB3027`
("could not copy ... the file is locked by ... AHC.Sandbox.Api") — always stop the running API
first, then build, then restart it if you need it running again.
