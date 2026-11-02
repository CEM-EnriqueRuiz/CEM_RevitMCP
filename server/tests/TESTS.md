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
| `tools/export_view_image.test.ts` | the `export_view_image` schema: an empty payload (the active view, `pixelSize` 1600 by default), a view by id or name, `pixelSize` 32-10000 integers only, `detailLevel` Coarse/Medium/Fine in any case and normalised, an unknown level rejected | The modelling agent checks families one view at a time, at each detail level (2026-11-01). Written first: the file failed to import until the tool existed. |
| `tools/family_open_session.test.ts` | the `family_open_session` schema: an empty payload, `familyName` with `hostDocument` kept, `familyPath`, a non-string `hostDocument` rejected | QA-2 (2026-11-01): a second open by name searched the active family's nested families; the host is now the open project or `hostDocument`. Written first: 2 of 4 red. |

## Connection target (security)

| File | Verifies | Why |
|---|---|---|
| `utils/connection.test.ts` | `ConnectionManager` pins `REVIT_HOST`=`127.0.0.1`, `REVIT_PORT`=8080, and `withRevitConnection` reaches a listener bound only to `127.0.0.1:8080` (skipped if 8080 is busy) | The Revit plugin binds IPv4 loopback only (`docs/decisions/0021-loopback-only-socket.md`); `localhost` may resolve to `::1` first. Mutation-checked: reverting the host to `localhost` fails the first test. |

## The socket's shared secret (decision 0022, SEC-5, 2026-11-01)

| File | Verifies | Why |
|---|---|---|
| `utils/auth.test.ts` | the real `RevitClientConnection` against a fake plugin on an ephemeral port: `params.auth` is the file's token (trimmed) on every request, with the tool's params unchanged; after a restart (a new token) the client reads the file again and retries once; a wrong token is refused after one retry with an error that names the file and never the token (nor does the console); a missing or empty file sends nothing and says to start the service and to run as the same Windows user (a sandbox cannot); the 120 s timeout is unchanged and not retried; the default path is `%LOCALAPPDATA%\Cemengal\RevitMCP\token`; the code is the SDK's `Unauthorized` (-33200) | Other local accounts (the Codex sandbox) can reach `127.0.0.1:8080`, which runs any C# through `send_code_to_revit` (`docs/decisions/0022-local-shared-secret-handshake.md`). 8 of 9 red first; the timeout test locks the existing path in. |
| `guards/socket-auth-pins.test.ts` | the plugin's C# (Revit-bound, 0016): 32 CSPRNG bytes per `Issue`; the path from `SpecialFolder.LocalApplicationData`, never `%TEMP%`/`CemTempFolder`/ACC; two protected ACLs granting only the user's SID and LocalSystem; a foreign-owned folder refused; a `CreateNew` temp file protected by path before its content, then `MoveFileEx(REPLACE_EXISTING)`; reparse checks before and after; `Issue` after the bind and before the accept thread, `Revoke` on `Stop`, a write failure stopping the listener; `Initialize` subscribing `ApplicationClosing` once, its handler calling `Stop`, `Stop` removing it from the same instance, the plugin's `OnShutdown` path kept; `Matches` before `CommandExecutor`, `Unauthorized` with no detail and the request's id, `auth` removed before dispatch; the compare loops over every expected byte with no early exit; the null token refuses all; loopback only; no token variable in any log, trace or exception in `plugin/` or `commandset/`; no raw-request trace; the rejection logged by method; `ActionLogger` drops `auth` | The same decision. 14 of the first 16 red first; the loopback pin and the leak scan already held (the scan was mutation-checked against sample leaks); the owner pin came with its code. The two closing pins were added after the live check found the token file outliving Revit (CEM_RibbonUI has no `OnShutdown`), red first. The fix batch (2026-11-02) added three, red first: the POSIX rename and bounded retry, the escaped log arguments, and the escaped refused method. The second batch (2026-11-02) added one, red first: `ExclusiveAddressUse = true` on each new listener before `Start` (SEC-5.2, CWE-605). `no-acc-writes` gained the token writer as its one listed exception outside `CemTempFolder` (red first). |
| `guards/log-format.test.ts` | every `_logger.Info/Warning/Error/Debug` message in `plugin/` is a string literal and a valid .NET composite format string for its argument count: `{{`/`}}` escapes, `{index[,alignment][:format]}` items, no lone brace, no index without an argument; the parser itself is checked on the bug it guards against | 0022, the second SEC-5 batch (2026-11-02). Red first on `CommandManager.cs:72` (`({1}}`, a `FormatException` whenever a command's supported versions excluded the running Revit); fixed to `({1})`. |
| `plugin/runtime.test.ts` (+ `fixtures/plugin-runtime.ps1`, `fixtures/token-reader.cjs`) | the built net48 plugin DLL, no Revit: `LogText.Escape` turns CR, LF, tab, the other C0/C1 controls, DEL and U+2028/U+2029 into visible escapes, keeps ordinary text, is idempotent; `EscapeArgs` keeps numbers formattable; 200 `McpAuthToken.Write` calls into a scratch folder all succeed while a Node `readFileSync` loop and a no-delete-share .NET reader keep opening the file; no partial read, the file never vanishes, the ACL is user + SYSTEM, no temp file left; the DLL is not older than its sources | The SEC-5 fix batch (2026-11-02): CWE-117 log forging, and the token write that failed with Win32 error 5 while a reader held the file (199 of 200 red first; both halves of the fix mutation-checked). Skips without the built DLL. |

## No writes under ACC or beside the DLL (root decision 0031)

| File | Verifies | Why |
|---|---|---|
| `guards/no-acc-writes.test.ts` | no ACC path in `plugin/`, `commandset/`, `server/src` or the csproj; no write to the raw `%TEMP%`, Desktop or Documents; no Node file write; no writer that derives its path from the add-in folder (`Assembly.Location`, `PathManager.`); every C# writer is on the inventory and takes its path from `CemTempFolder`; the action log, plugin log and screenshots in `%TEMP%\Cemengal\RevitMCP`; `create_project_parameter` restores the user's shared parameter file in `finally`; `family_save_session` never saves in place under ACC; `create_shared_parameter` works on a copy of a team shared parameter file on ACC and restores it; both projects reference `CEM_CSharpExtended` | The plugin loads from the ACC menu on colleagues' machines, so any write beside it or under ACC is synced to everyone (`docs/decisions/0026-logs-and-artefacts-in-local-temp.md`). The C# runs only in Revit (exempt), so the guard reads its source. Written first: 14 of 15 red, one per write found in the 2026-10-24 audit; the 16th (shared parameter file on ACC) was found later, added with its fix, and shown red by removing the ACC check. Mutation-checked: a planted writer with an ACC path and a `PathManager` path fails three tests; dropping the ACC check in `family_save_session` fails one. |

## Fixes for the modelling agent (2026-11-01)

| File | Verifies | Why |
|---|---|---|
| `guards/modeller-pins.test.ts` | `family_open_session` activates the family through `FamilySessionHelpers.Activate` (`OpenAndActivateDocument`, a pathless family saved to `Families\` first), and `family_save_session` re-activates the project before `Close(false)`; `send_code_to_revit` writes the exact code to `code\` before the raise, abandons a timed-out run (`TryAbandon`), returns `codeFile`/`transactionMode`/`documentTitle`/`isFamilyDocument`/`outcome`/`durationMs`, reports a non-`Committed` commit as rolled back, appends a `late` line, never `Reset()`s in the wait, and the log points at `result.codeFile`; `export_view_image` rolls its transaction group back in `finally` (never `Commit`/`Assimilate`), writes a new `images\` file, and is tagged `access: read` (every tag read or write) | The C# is Revit-bound (0016), so the source is pinned (`docs/decisions/0027-send-code-scripts-traced-to-local-temp.md`, `docs/modules/family-editing.md`, `docs/modules/toolset.md`). Written first: all 14 red, with the no-acc-writes inventory test red for the two new writers. |
| `guards/modeller-pins.test.ts`, the review fix batch (2026-11-01) | QA-2: the family is resolved by name in `ResolveHost` (`hostDocument`, else a non-linked project, else the active document) and never in `doc`; QA-5: no `OverwriteExistingFile = true`, and copies come from `NewFamilyCopyPath` (`NewArtifactFile` in `Families`); `familyPath` reuse by full path (`FindOpenFamilyByPath`); QA-3: no `Reset()` in `export_view_image`'s wait, and the request setter re-arms it; SEC-2: `MakeExportNameSafe`, then `ImageExportOptions.GetFileName` checked by `IsDirectlyIn`, before `ExportImage`, all inside the group; QA-6: `ApplyDetailLevel` and `MakeExportNameSafe` only inside the rolled-back group, and the view's `DetailLevel` is assigned in one place; SEC-6: the run id is `Guid.NewGuid()`, never a caller's field or the request id; the "never ran" line is claimed once (`TryClaimNeverRan`) by the handler or the next call; both skills give `<n> MCP tools` and `<n> commands` from the registry | The QA and security quick reviews (0 blocking); the C# is Revit-bound (0016). Written first: 9 red; the SEC-6 pin passed on the existing code and locks it in. Two earlier pins were corrected: the Families copy path (QA-5 changes it on purpose), and a `DetailLevel` count that matched the result DTO. QA-7 (the QA recheck) added 2 pins, both red first: `ResolveHost` returns the active document when it is a project, before `FindProjectDoc`, and the tool description states that order. The SEC-8 follow-up added 4 pins, all red first: `McpLocalFiles.IsReparsePoint`/`RefuseReparsePoint` (`FileAttributes.ReparsePoint`, `CemTempFolderUnsafeException`); `NewFamilyCopyPath` checks the folder before `CreateDirectory` and the folder and file after; `Activate` turns a refusal into an `InvalidOperationException` before any save; the fixed shared-parameter file and its folder are checked before and after the write. |
