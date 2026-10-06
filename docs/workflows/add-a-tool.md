---
title: Add (or change) an MCP tool
type: workflow
updated: 2026-10-06
sources: [CEM_RevitMCP.md, .claude/skills/cem-revitmcp-dev/SKILL.md, command.json, server/tests/TESTS.md]
related: [../concepts/four-part-tool-pattern.md, ../concepts/ai-grade-conventions.md, ../concepts/domains-and-naming.md, test.md, build-and-deploy.md, smoke-test.md, finish-a-change.md]
tags: [workflow, tools]
---

# Add (or change) an MCP tool

## 0. Decide whether the tool should exist

- Check existing tools first ([toolset](../modules/toolset.md)); a per-namespace audit found most
  "missing" capabilities already covered.
- Prefer evidence: what does the AI hand-write in `Recipes\`
  ([0008](../decisions/0008-parent-tools-from-recipe-corpus.md))?
- Respect the scope line ([0006](../decisions/0006-create-methods-not-full-parity.md)): no full
  parity, and no rebar/loads/analytical, blend/sweep, or scope box.
- Pick the domain from the API namespace of the class
  ([0003](../decisions/0003-domains-from-api-namespaces.md)). That gives the prefix and folder
  ([0004](../decisions/0004-prefix-equals-folder-equals-namespace.md)).

## 1. Write the schema test first (TDD)

In `server/tests/tools/<name>.test.ts`, copy an existing test. Use
`captureToolSchema(register<Name>Tool, "<name>")` and assert: minimal valid parses, fully specified
valid parses, and each invalid shape fails. Run it and watch it fail (the tool file doesn't exist
yet). Add a line to `server/tests/TESTS.md`. The C# side is exempt from TDD here: it needs a live
Revit, so state that exemption in your report ([0016](../decisions/0016-schema-only-ts-unit-tests.md)).

## 2. The four parts

1. `commandset/Models/<Domain>/<Name>Models.cs` (or `Models/Common`): request/result DTOs with
   `[JsonProperty("camelCase")]`. Lengths are in mm.
2. `commandset/Services/<Domain>/<Name>EventHandler.cs`: `IExternalEventHandler,
   IWaitableExternalEventHandler`. Own the `Transaction` (on the family doc for family geometry),
   skip-and-warn per item, return `AIResult<T>` with a rich `Message`, and call `_resetEvent.Set()`
   in `finally` ([threading](../concepts/threading-and-external-events.md)).
3. `commandset/Commands/<Domain>/<Name>Command.cs`: `ExternalEventCommandBase`, compact template.
   **No Revit API calls here** ([0002](../decisions/0002-command-raises-external-event.md)).
4. `server/src/tools/<name>.ts`: zod shape mirroring the DTO, every field `.describe()`d,
   `withRevitConnection(c => c.sendCommand("<name>", params))`, and an exported
   `register<Name>Tool`. No list to edit.
5. `command.json`: `{ "commandName": "<name>", "description": "…", "assemblyPath":
   "CEM_IAModeler_CommandSet.dll" }`.

## 3. Check the three names

```bash
grep -rn '"<name>"' server/src/tools/<name>.ts commandset/Commands command.json   # expect 3+ hits
```

## 4. Verify, then finish

- `cd server && npx vitest run` passes, and `npm run build` is clean ([test](test.md)).
- Build the command set and redeploy through CEM_RibbonUI ([build and deploy](build-and-deploy.md)).
- Run the [smoke test](smoke-test.md) for the new tool. Report what you drove separately from what
  is asserted.
- Update the wiki: [toolset](../modules/toolset.md) (count and list), affected decision/module
  pages, and the log ([finish a change](finish-a-change.md)). `CEM_RevitMCP.md` §5/§7 also lists
  tools. Either update it or record that it is now a legacy document
  ([0019](../decisions/0019-single-authoritative-doc.md)).

## Pre-flight checklist

- [ ] Four files in the matching domain folder, plus a `command.json` entry
- [ ] Three names byte-identical
- [ ] No Revit API in the Command; the handler owns its Transaction and sets the event in `finally`
- [ ] mm in/out; `AIResult<T>` with a human-readable `Message` and `⚠ Warnings`
- [ ] Batch skip-and-warn; names and enums both accepted; idempotent by name
- [ ] Multi-version safe (`ElementIdExtensions`, `REVIT20xx_OR_GREATER`)
- [ ] Schema test written first; `npx vitest run` green; `npm run build` clean
- [ ] Wiki updated in the same commit
