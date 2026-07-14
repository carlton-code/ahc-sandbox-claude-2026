---
description: Build the solution, run the NUnit test suite, and review the pending changes against this repo's architecture and SQL-safety conventions before committing.
---

# Verify the current changes in AHC.Sandbox before they're committed:

1. Run `dotnet build` from the repo root and fix any compile errors.
2. Run `dotnet test` and report any failures (see `.claude/agents/test-runner.md` for how to
   diagnose them).
3. Check `git status`/`git diff` to identify what changed this session, and review it against
   `.claude/agents/architecture-reviewer.md`'s checklist
   (layering, DI placement, controller thinness, naming/route conventions).
4. If any changed file touches raw SQL/`DbCommand` in `AHC.Sandbox.Data`, also review it against
   `.claude/agents/sql-safety-reviewer.md`'s checklist (parameterization, schema correctness,
   connection lifecycle).

Summarize as a short pass/fail list per step — don't re-explain the whole codebase, just report
what's wrong (if anything) and where.
