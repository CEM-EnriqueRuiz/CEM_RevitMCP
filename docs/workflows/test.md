---
title: Run the tests
type: workflow
updated: 2026-10-06
sources: [server/package.json, server/vitest.config.ts, server/tests/TESTS.md, tests/commandset/CEM_IAModeler_Test.csproj]
related: [../modules/tests.md, ../decisions/0016-schema-only-ts-unit-tests.md, smoke-test.md]
tags: [testing, tdd]
---

# Run the tests

## Fast loop (pure, always run)

```bash
cd server && npx vitest run       # or: npm test
```

From the Cemengal root, the same suite runs with `.\.claude\run-tests.ps1 -Repos revitmcp`.

## TDD here

- New TS tool → write its schema test first ([add a tool](add-a-tool.md) step 1).
- `withRevitConnection` and the socket are a documented gap; don't mock them.
- C# EventHandler logic needs a live `Document`. That is the stated TDD exemption ("Revit-bound").
  Say so in your report rather than skipping silently.

## Integration tier (manual, needs licensed Revit)

`tests/commandset/` (`Nice3point.TUnit.Revit`, upstream). Run it from the IDE or test runner with
Revit available. It is not part of the fast loop or `run-tests.ps1`.

## Not a test

Driving Revit through the `cem-revit-ai` MCP server while developing is useful, but it is not
coverage. Report "what I drove" and "what is asserted" as separate claims. Never put MCP calls in a
test project. The [smoke test](smoke-test.md) is a manual end-to-end check.

## Never ship tests

Test folders, `vitest.config.ts` and `TESTS.md` must stay out of any package. `server/package.json`
`files` ships only `build/` and `README.md`; the `.mcpb` uses `server/.mcpbignore`.
