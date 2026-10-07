---
name: cem-revitmcp-dev
description: Add or modify tools in the CEM Revit MCP server — the 4-part pattern (TS tool + C# Command + EventHandler + Models) wired through command.json and a localhost socket. Use when adding an MCP tool that drives Revit, or changing the command set / server. Triggers "MCP tool", "Revit MCP", "add a command to the MCP server", "CEM_AIModeler", "send_code_to_revit".
---

# CEM_RevitMCP development

CEM_RevitMCP is a **fork of `mcp-servers-for-revit`** that lets an AI agent drive Revit. A
TypeScript MCP server (`server/`, 118 tools) talks JSON-RPC over TCP :8080 to a C# plugin
(`plugin/`, CEM_IAModeler) that dispatches to a command set (`commandset/`,
CEM_IAModeler_CommandSet, 120 commands registered in `command.json`). It is hosted and deployed by
CEM_RibbonUI in the sibling `CEM_RevitAPI` repo. Upstream code is marked as such in the wiki; extend
the fork through its four-part pattern rather than reworking upstream plumbing.

**Before planning or editing, read [docs/index.md](../../../docs/index.md) ("Start here"), then
the pages it names.** Search with qmd when the index is not enough (MCP tools, or
`qmd query -c cem_revitmcp "..."`). The wiki holds the detail; this skill only holds the hard rules.
Also read [CROSS_REPO.md](../../../../../CROSS_REPO.md).

## Hard rules

1. A tool = `server/src/tools/<name>.ts` + `commandset/{Commands,Services,Models}/<Domain>/` + a `command.json` entry → [0001](../../../docs/decisions/0001-extend-via-four-part-pattern.md)
2. `server.tool("x")`, `CommandName => "x"` and `command.json` `commandName` are byte-identical; a mismatch fails silently → [0001](../../../docs/decisions/0001-extend-via-four-part-pattern.md)
3. The Command never calls the Revit API; the EventHandler owns the Transaction and signals in `finally` → [0002](../../../docs/decisions/0002-command-raises-external-event.md)
4. Domains from API namespaces; tool prefix == folder == namespace suffix → [0003](../../../docs/decisions/0003-domains-from-api-namespaces.md), [0004](../../../docs/decisions/0004-prefix-equals-folder-equals-namespace.md)
5. mm in/out, `AIResult<T>` with a readable `Message`, skip-and-warn batches, names and enums, idempotent → [0005](../../../docs/decisions/0005-ai-grade-tool-conventions.md)
6. Create methods plus common edits, not parity; the long tail goes to `send_code_to_revit` → [0006](../../../docs/decisions/0006-create-methods-not-full-parity.md)
7. Choose new tools from the recipe corpus → [0008](../../../docs/decisions/0008-parent-tools-from-recipe-corpus.md)
8. Family geometry on the family doc; small tools; no blend/sweep → [0009](../../../docs/decisions/0009-small-composable-family-tools.md), [0010](../../../docs/decisions/0010-no-newblend-use-sloped-extrusions.md)
9. CEM_RibbonUI owns deployment; rebuild it with Revit closed → [0011](../../../docs/decisions/0011-cem-ribbonui-hosts-deployment.md)
10. The code wins over `CEM_RevitMCP.md` (legacy, partly stale) → [doc drift](../../../docs/concepts/doc-drift.md)
11. `send_code_to_revit` scripts are not persisted: no `Recipes\` dump (removed as noise, don't reintroduce), and the action log omits the body → [0023](../../../docs/decisions/0023-send-code-scripts-not-persisted.md)

The step-by-step procedure is in [add a tool](../../../docs/workflows/add-a-tool.md).

## Verify

```bash
cd server && npx vitest run      # schema tests; write the new tool's test first
cd server && npm run build       # build/index.js, what the MCP client runs
```

```powershell
dotnet build commandset\CEM_IAModeler_CommandSet.csproj -c "Debug R24" -p:Platform=x64 -p:DeployRevitAddin=false
```

Then rebuild CEM_RibbonUI with Revit closed and run the
[smoke test](../../../docs/workflows/smoke-test.md). C# handler logic is Revit-bound: state the TDD
exemption rather than skipping silently. Driving Revit through MCP is not a test.

## Finish (commit-time ingest)

Commit only after the user sends the issue URL. In the **same** commit, ingest the change into
`docs/`: the source page (`sources/issues/<org>-<repo>-<N>.md`, or `sources/commits/<YYYY-MM>.md`)
with every decision, its reason and the rejected alternatives; the affected pages (new tools →
`modules/toolset.md`); `index.md`; and a `log.md` entry. `python ../../tools/wiki/wiki_lint.py .`
must show 0 errors. Message: one line, `FEAT|FIX|REFACTOR - Description. <issue URL>`, no body.
Stage explicit paths; `.claude/` is gitignored (`git add -f`). See
[finish a change](../../../docs/workflows/finish-a-change.md).
