# server/ test suite

TypeScript-side unit tests, added because `server/` had no test runner before this pass
(`package.json` had no `test` script). Runs with [vitest](https://vitest.dev) via `npm test`
(alias for `vitest run`) from `server/`.

## Why this suite is schema-only

Almost all logic in `server/src/tools/*.ts` is a thin wrapper: build a params object, then
`withRevitConnection(c => c.sendCommand(name, params))`, which opens a real TCP socket to
`localhost:8080` (`server/src/utils/SocketClient.ts`, `ConnectionManager.ts`). That is not
separable from a live socket without a mock heavier than this pass's scope, and there is no
other genuinely pure helper in `server/src/` — `SocketClient`'s buffer/response handling is
entangled with instance state and the live socket, not pure.

What **is** pure and real is each tool's inline zod schema — the request-shape contract the
LLM fills in before a call ever reaches the socket. `tests/helpers/captureToolSchema.ts` stubs
just enough of `McpServer` (`{ tool(name, description, schemaShape, handler) }`) to import the
*actual* `register*` export from a real `server/src/tools/<name>.ts` file and recover the exact
zod shape shipped in production — not a hand-copied mirror that could drift from source.

## What's covered

Five tools, one per representative domain (per `CEM_RevitMCP.md` section 2's domain map):

| File | Tool | Domain | Notable schema features exercised |
|---|---|---|---|
| `tools/create_level.test.ts` | `create_level` | Core (flat) | array-of-objects, required vs. optional vs. defaulted fields |
| `tools/family_add_parameter.test.ts` | `family_add_parameter` | `family_*` | zod `.enum()` validation, `.default()` values |
| `tools/arch_create_wall.test.ts` | `arch_create_wall` | `arch_*` | `.array().min(1)`, nested point/segment objects |
| `tools/mep_create_duct.test.ts` | `mep_create_duct` | `mep_*` | optional-vs-required interplay (round vs. rectangular duct fields) |
| `tools/viz_create_material.test.ts` | `viz_create_material` | `viz_*` (`DB.Visual`) | numeric bounds (`.min()`/`.max()`/`.int()`), fixed-length arrays (`colorRgb` length 3) |

Each file asserts: a minimal valid payload parses, a fully-specified valid payload parses,
and each documented invalid shape (missing required field, wrong type, out-of-range /
out-of-enum value) fails `.safeParse()`.

## Traceability

None of these five tool files were introduced by a Cemengal-authored commit — the only three
Cemengal-authored commits on top of the upstream fork are `b05a76f`, `aa88b96`, `b04b41e`, and
none of them touch `server/src/tools/*.ts` (they added `CLAUDE.md`, the skill, and the initial
`CEM_AIModeler`/tool-set scaffolding docs — see `git show --stat <hash>`). So these tests are
not regression tests against a specific Cemengal FIX/FEAT commit; they anchor instead to the
architecture doc and skill sections that describe *why* the schema contract matters:

- `CEM_RevitMCP.md` section 4, "The anatomy of one tool" — "TS tool zod schema mirrors the C#
  DTO field-for-field (describe every field — the LLM reads them)".
- `CEM_RevitMCP.md` section 4, "Conventions" #1 (mm in/out) and #8 (`verb_noun` naming).
- `.claude/skills/cem-revitmcp-dev/SKILL.md`, "Decisions and rationale" — "Small composable
  tools beat a monolith" (motivates why `family_add_parameter` is its own small schema rather
  than folded into a bigger tool).

## Known doc-drift found while writing these tests (not fixed here — reported upstream)

`.claude/skills/cem-revitmcp-dev/SKILL.md`'s "Decisions and rationale" section describes a
"deliberately kept" TS-only local SQLite pack (`store_project_data` / `store_room_data` /
`query_stored_data`, backed by `server/src/database/`). That source does not exist in this
tree: `server/src/` has no `database/` directory and no such tool files, and
`git log --all --diff-filter=A -- "server/src/database/*"` returns nothing — it was never
committed. `CEM_RevitMCP.md` section 5 is actually up to date and correct on this point (it has
a "Removed" line documenting the SQLite pack's removal); only the skill's "Decisions and
rationale" section is stale. A leftover `server/revit-data.db` (gitignored) is still present on
disk as a relic of that removed feature. See this task's final report for the full write-up;
this file only records it for anyone reading the test suite later.

## Cemengal tools

| File | Verifies | Why |
|---|---|---|
| `tools/cem_tools.test.ts` | the schemas of the six `cem_*` tools: `cem_open_tool` (optional label), `cem_swap_find_types` (positive integer cap), the two no-argument tools, `cem_swap_run` (preset or an inline swap in the `SwapPresets.json` shape; `sources` min 1 and `target` required; the add-in's enums: scopes, comparisons, placement modes, trial/all) and `cem_rules_apply` | The inline swap is parsed in C# by CEM Swap's own `SwapPresetStore`; the schema keeps the LLM inside the shape and enums that parser accepts (`docs/decisions/0025-cemengal-addins-as-tools-via-project-references.md`). Written before the tool files (12 tests, failing first). |

## Connection target (security)

| File | Verifies | Why |
|---|---|---|
| `utils/connection.test.ts` | `ConnectionManager` pins `REVIT_HOST`=`127.0.0.1`, `REVIT_PORT`=8080, and `withRevitConnection` reaches a listener bound only to `127.0.0.1:8080` (skipped if 8080 is busy) | The Revit plugin binds IPv4 loopback only (`docs/decisions/0021-loopback-only-socket.md`); `localhost` may resolve to `::1` first. Mutation-checked: reverting the host to `localhost` fails the first test. |
