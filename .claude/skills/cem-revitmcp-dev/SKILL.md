---
name: cem-revitmcp-dev
description: Add or modify tools in the CEM Revit MCP server — the 4-part pattern (TS tool + C# Command + EventHandler + Models) wired through command.json and a localhost socket. Use when adding an MCP tool that drives Revit, or changing the command set / server. Triggers "MCP tool", "Revit MCP", "add a command to the MCP server", "CEM_AIModeler", "send_code_to_revit".
---

# Adding tools to CEM_RevitMCP

This is a **fork** of `mcp-servers-for-revit`. It exposes Revit to an AI agent as MCP tools over a
localhost socket.

## Read the authoritative doc first

**[CEM_RevitMCP.md](../../../CEM_RevitMCP.md) is the source of truth.** It is a single merged
document (four older plan docs were consolidated into it and deleted) covering architecture,
domains, folder structure, the tool anatomy with canonical templates, the full toolset list, DB
coverage, build order, and the smoke test.

This skill does not restate it. It exists to surface the handful of facts that are most often got
wrong, and to give you the workflow around the doc.

Also read [CROSS_REPO.md](../../../../../CROSS_REPO.md) — this is a fork, so upstream-divergence
discipline applies.

**Write new pure logic test-first, where it exists.** Most of this repo's TS logic wraps a live
socket to Revit (`withRevitConnection`) and isn't unit-testable in isolation — that's the norm here,
not a gap to apologize for. The one real testable surface is each tool's zod schema
(`.safeParse()`), and the existing `Nice3point.TUnit.Revit` C# suite (`tests/commandset/`, needs
live Revit) is a different tier entirely. See
[reference/tdd-and-test-runner.md](../../../../../.claude/skills/cemengal-architect/reference/tdd-and-test-runner.md)
for the workflow and its exemptions.

## The three facts that break builds

**1. The 4 parts.** A tool is four files plus one registry entry:

| Part | Path | Role |
|---|---|---|
| TS tool | `server/src/tools/<name>.ts` | `server.tool(...)` → `withRevitConnection(c => c.sendCommand(...))` |
| C# Command | `commandset/Commands/<Domain>/<Name>Command.cs` | `ExternalEventCommandBase`, raises the event and blocks |
| C# EventHandler | `commandset/Services/<Domain>/<Name>EventHandler.cs` | `IExternalEventHandler` — the actual Revit work |
| C# Models | `commandset/Models/<Domain>/<Name>Models.cs` | request/result DTOs with `[JsonProperty]` |
| Registry | `command.json` (repo root) | `{ "commandName": ..., "assemblyPath": "RevitMCPCommandSet.dll" }` |

**2. Three names must match byte-for-byte:**

```
server.tool("my_command", ...)          // TypeScript
public override string CommandName => "my_command";   // C#
{ "commandName": "my_command", ... }    // command.json
```

Runtime resolution is by `CommandName` string only (reflection in `CommandManager`), so folders and
namespaces are purely organizational — but a mismatch in these three strings means the tool silently
never resolves.

**3. The Command must NEVER call the Revit API directly.** The socket runs on a background thread;
the Revit API is only valid on the UI thread. The Command raises an `ExternalEvent` and blocks
(`RaiseAndWaitForCompletion(20000)`); only the EventHandler touches Revit, owns the `Transaction`,
and signals completion in a `finally`.

Violating this produces intermittent, hard-to-diagnose failures rather than a clean error.

## Registration is automatic on the TS side only

`server/src/tools/register.ts` `readdirSync`s the tools directory, dynamically imports every file,
and calls any export whose name `startsWith("register")`. **Drop the file in — no list to edit.**

The C# side is the opposite: `command.json` is a hand-maintained registry and is the authoritative
count of what exists.

```bash
grep -o '"commandName"[^,]*' command.json      # regenerate the tool list
grep -c '"commandName"' command.json           # the real count (120 today)
```

**Planning docs have historically lagged the code — `command.json` is truth.** When a batch lands,
update `CEM_RevitMCP.md` §5 (toolset list/count) and §7 (build order).

## Naming and folders

- Core tools: flat `verb_noun` (`create_wall`, `tag_elements`).
- Domain tools: `domain_verb_noun` — `mep_*`, `arch_*`, `family_*`, `coord_*`, `struct_*`, `viz_*`,
  `parent_*`.
- **Folder name == namespace suffix == the `domain_` tool prefix.** Canonical domains: `Core`,
  `Access`, `Architecture`, `Mep`, `Family`, `Struct`, `Views`, `AnnotationComponents`,
  `DataExtraction`, `Delete`, `Test`, `ExecuteDynamicCode`, `ParentTools` (+ `Models/Common`).

Commands, Services and Models all mirror the same domain subfolders.

*Gotcha:* NTFS case-only renames (e.g. `MEP` → `Mep`) need a two-step rename to take effect on disk.

## Non-negotiable conventions

From the doc's "Conventions" section — these are what make tools AI-grade:

- **mm in, mm out.** Unit conversion happens at the boundary.
- Return `AIResult<T>` with a **human-readable `Message`** — the model reads it.
- **Never hard-fail a batch.** Skip, warn, continue.
- Accept **both** display names and `BuiltInParameter` / `BuiltInCategory` enum names.
- **Idempotent by name** where creation is involved.
- One Command + one EventHandler per tool.
- Multi-version guards (`REVIT2022_OR_GREATER`) and `ElementIdExtensions` instead of
  `new ElementId(int)`.

## Verify

**Unit tests: `npx vitest run`** (in `server/`). A real suite validates each tool's zod schema
(`.safeParse()` against minimal-valid, fully-specified-valid, and each documented invalid shape) for
a representative tool per domain — the one genuinely pure, unit-testable surface here. Copy its
shape for a new tool's schema tests; see `server/tests/TESTS.md`. This does **not** touch
`withRevitConnection` or anything socket-bound — that stays a documented gap. The separate
`tests/commandset/` C# suite (`Nice3point.TUnit.Revit`) needs a live Revit process and is not run
by this command.

Build the TS server and the C# command set, then run the smoke test in
[CEM_RevitMCP.md](../../../CEM_RevitMCP.md) §8 to exercise the full chain.

```bash
cd server && npm run build       # emits to build/, which the .mcpb and manifest run from
```

The transport is JSON-RPC over a TCP socket on localhost:8080 (`SocketService` C# side,
`SocketClient.ts` JS side). The client buffers until the whole JSON parses, so large payloads
(base64 images) work — but keep them bounded.

To return an image, the TS tool returns an MCP content block:
`{ type: "image", data: <base64>, mimeType: "image/png" }`.

## Pre-flight checklist

- [ ] All four files created in the matching domain subfolder
- [ ] `command.json` entry added
- [ ] The three names match byte-for-byte
- [ ] Command raises an ExternalEvent; **no Revit API call in the Command**
- [ ] EventHandler owns its `Transaction` and signals completion in `finally`
- [ ] mm in / mm out; `AIResult<T>` with a human-readable `Message`
- [ ] Batch operations skip-and-warn rather than throwing
- [ ] `CEM_RevitMCP.md` §5/§7 updated with the new tool and count
- [ ] New tool's zod schema has a test in `server/tests/` written before/alongside the schema (or the exemption is stated)
- [ ] `npx vitest run` green in `server/`
- [ ] `npm run build` in `server/` clean; smoke test passes

## Decisions and rationale

**Domains are derived from the Revit API CHM namespace inventory, not wishlist words.**
`commandset/Utils/RevitAPI_2024_namespaces.md` is the map. When someone says
"views/styles/sheets/schedules/tagging", check which namespace the class actually lives in first —
views, sheets, `ViewSchedule`, `IndependentTag`, dimensions, `TextNote`, `FilledRegion`,
`GraphicsStyle` **all live in `Autodesk.Revit.DB`**, so they extend the **Core** pack and are not
separate domains. The only real appearance namespace is `Autodesk.Revit.DB.Visual` → `viz_*`.

**Don't chase full property parity.** Past ~100 tools, LLM tool-selection degrades. The target is
the ~151 DB create-methods plus common edits; reads stay generic via `get_element_*` / `find_elements`.

**Some APIs have no public create path** (scope box, portable clash). Document and defer to
`send_code_to_revit` rather than shipping a guess. Deliberately excluded: rebar/loads/analytical
(fragile, version-sensitive), conceptual mass, site, model text, host sweeps, ExtensibleStorage.

**Always check existing tools before adding.** A per-namespace audit found Selection was already
covered (`operate_element` does `Selection.SetElementIds`) and Events correctly absent (host
subscriptions, not tools).

**The real capability gap was reading, not authoring.** Analysis of the recipe corpus (the C#
payloads the AI emits via `send_code_to_revit`) showed ~67% was link-geometry reconstruction:
read geometry from a linked IFC → measure → recreate as native Revit elements → validate deviation.
The existing tools were all *authoring* primitives taking clean numeric input, so the AI fell back
to raw code for the whole pipeline. That drove the `parent_*` pack
(`parent_link_extract_geometry`, `parent_solid_to_member_params`, `parent_reconstruct_native`,
`parent_validate_deviation`). **When asked what to build next, look at the recipe corpus first.**

**Small composable tools beat a monolith.** An earlier monolithic `parent_family_build_geometry`
approach and its orphan model file were abandoned in favor of small tools
(`family_open_session`, `family_save_session`, an upgraded `family_create_extrusion`).
`NewBlend`/`NewSweep` stay excluded — the recipes proved blend fights the API; decompose into sloped
extrusions instead. Family geometry transactions must be on the **family doc**, not the project.

**Two on-disk audit trails** exist under a hardcoded ACCDocs CEMAIModeler folder, both fail-soft and
marked `TODO: make configurable`: `Screenshots\` (per `take_screenshot`) and `Log\` (one JSONL line
per *every* tool call, written at `CommandExecutor.ExecuteCommand` — the single dispatch chokepoint;
`send_code_to_revit` bodies are omitted). The per-call `Recipes\` dump of every script was removed as
noise — don't reintroduce it. To change the log,
edit `plugin/Utils/ActionLogger.cs` — **not** `PathManager`, which points at a different
plugin-internal `Logs` dir.

**`System.Drawing` collides with `Autodesk.Revit.DB`** (both define `Rectangle`/`Color`) — alias the
GDI types. On net8 (R25/R26) it needs the `System.Drawing.Common` package; on net48 it is a
framework reference.

**Deliberately kept:** `store_project_data` / `store_room_data` / `query_stored_data` are a TS-only
local SQLite pack that never touches Revit. Don't "fix" them by adding a C# side.
