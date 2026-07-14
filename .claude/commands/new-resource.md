---
description: Scaffold a new CRUD resource end-to-end across Domain/Application/Data/Api, following the Customer vertical slice pattern.
---

# Scaffold a new resource for the AHC.Sandbox solution: **$ARGUMENTS**

If the arguments don't already specify both a resource name and its backing `SalesLT` table,
ask for whichever is missing before starting.

Follow the `api-scaffolder` agent's process (`.claude/agents/api-scaffolder.md`) — read it and
either delegate to that agent or follow the same steps directly: Domain entity → Application
DTOs/interfaces/service → Data EF entity+mapping+repositories → Api controller, registering each
new service/repository in its own layer's `DependencyInjection.cs`.

Run `dotnet build` at the end to confirm it compiles. Don't add tests unless asked.
