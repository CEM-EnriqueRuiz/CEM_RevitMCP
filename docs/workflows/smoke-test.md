---
title: Smoke test (full chain, live Revit)
type: workflow
updated: 2026-10-06
sources: [CEM_RevitMCP.md, b04b41e]
related: [build-and-deploy.md, test.md, ../modules/family-editing.md, ../concepts/audit-trails.md]
tags: [verify, smoke-test]
---

# Smoke test (full chain, live Revit)

This is a manual end-to-end check, not a test ([test](test.md)). It was originally `SMOKE_TEST.md`
(`b04b41e`) and was merged into `CEM_RevitMCP.md` §8.

## Prereqs

Revit 2024 with a scratch **project** (and optionally an **.rfa**). `server/build/index.js` built
(`cd server && npm install && npm run build`). Command set deployed
([build and deploy](build-and-deploy.md)).

## Point the MCP client at the local build

The published npm package lacks the Cemengal tools.

```json
{ "mcpServers": { "cem-revit-ai": {
  "command": "node",
  "args": ["<Cemengal checkout>\\CEM_IA\\CEM_RevitMCP\\server\\build\\index.js"] } } }
```

Claude Code: `claude mcp add cem-revit-ai -- node "<…>\CEM_IA\CEM_RevitMCP\server\build\index.js"`.
The old doc's path has an extra `mcp-servers-for-revit\` level, which is stale.

## Start the Revit side

Open a project, then Cemengal ribbon → **CEM_RevitAI** ("Open Server": listening on 8080, commands
loaded). Leave the project active.

## Run, in order (read-only first)

- **A.** `get_warnings`; `family_get_types` (OST_Doors); `get_element_parameters` of a selected
  element.
- **B.** `create_view` (FloorPlan); `arch_create_wall` (0,0,0)→(5000,0,0) h3000;
  `transform_elements` to copy it; `mep_create_pipe` (needs pipe and system types).
- **C. Family:** `family_open_session` on an .rfa → `family_create_extrusion` (try a hollow profile
  on a sloped `planeDef` with a `material`) → `family_save_session(save, loadIntoProject)`. In a
  project doc, the editor-only tools should fail clearly.
- Plus the new tool you are verifying, and `take_screenshot` to see the result.

**Pass:** read-only calls return data; creation returns `Success: true` plus ids that actually
appear in Revit; family-editor tools succeed in an .rfa and fail clearly in a project. Afterwards,
`Log\actions_*.jsonl` shows `family_*`/`arch_*` calls rather than `send_code_to_revit`
([audit trails](../concepts/audit-trails.md)).

## Triage

| Symptom | Cause |
|---|---|
| "connect to revit client failed" / 连接到Revit客户端失败 | server not started, or wrong port |
| tools missing in the client | client on the npm package or a stale build; use local `build/index.js`, `npm run build` |
| "Method not found" / command not found | registry not deployed, or a name mismatch among the three strings |
| "No <X>Type available" | the project lacks that type |
| 0 commands loaded | rebuild CEM_RibbonUI **with Revit closed** |
| connects, then every call says "Method not found" | a stray `CEM_IAModeler.addin` in the Addins folder serves an empty registry; delete it ([0024](../decisions/0024-standalone-deploy-only-without-ribbon-host.md)) |
| times out after 120 s | TS client timeout; the handler is slow or blocked by a modal dialog |
