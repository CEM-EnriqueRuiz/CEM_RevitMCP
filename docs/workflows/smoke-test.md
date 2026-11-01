---
title: Smoke test (full chain, live Revit)
type: workflow
updated: 2026-11-01
sources: [CEM_RevitMCP.md, b04b41e, https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82]
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

Claude Desktop on the development machine does the same since 2026-10-25: its `revit-mcp` runs
`C:\Program Files\nodejs\node.exe D:\repos\Cemengal\CEM_IA\CEM_RevitMCP\server\build\index.js` (124 tools, 6 `cem_*`).
Before, it ran the June 15 bundle copied to `C:\00-CMOT\…\05-CEMAIModeler\Server\index.js` (118 tools, no `cem_*`).
Colleagues' copy in ACC `05-CEMAIModeler\Server` stays the June bundle until the menu is published
([CEM_API#82](../sources/issues/CEM-EnriqueRuiz-CEM_API-82.md)).

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
`%TEMP%\Cemengal\RevitMCP\actions-<date>.jsonl` shows `family_*`/`arch_*` calls rather than `send_code_to_revit`
([audit trails](../concepts/audit-trails.md)). Nothing new appears beside the deployed DLLs or under ACC
([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)).

## Live check (2026-11-01, Revit 2024, after a CEM_RibbonUI Debug R24 deploy)

The calls went over the plugin's socket with the server's own client. The documents were new families from
`CMGL-GenericModel`; no project was opened, and nothing was saved beyond the temp copies below.

- **`family_open_session`, path branch:** the family went from inactive to active, and `family_add_reference_plane`
  then added its plane to that family only (4 planes against 3 in the other family).
- **`EditFamily` branch:** a nested new family (no file) was copied to `Families\Familia2.rfa` and activated, with
  the warning, and an editor tool acted on it.
- **`OpenAndActivateDocument` works from an `ExternalEvent` handler.**
- **`family_save_session close`:**
  - a family that is not active closed;
  - the active family, with no saved project open, stayed open with the warning;
  - re-activating a project before the close is not checked live, because no project was opened.
- **`send_code_to_revit`:**
  - `ok`, `rolledBack` (an `auto` snippet that created a plane and then threw; the plane count was unchanged) and
    `error` (a compile error, and a throw in `none` mode) each came back with their `codeFile`, and each file held
    the exact snippet;
  - a 75 s snippet got the "STILL RUNNING" timeout at 60.0 s. Its `"late": true` line followed at 75.4 s with
    `outcome: ok`, and its plane was in the model.
- **`export_view_image`** at Coarse, Medium and Fine, on a 3D view and on an elevation by id:
  - with the family unmodified before, it was still unmodified after, and the view kept its own level (Medium);
  - a box visible only at Fine came out halftone grey at Coarse and black at Fine. The Family Editor greys
    geometry hidden at the view's level rather than hiding it.
- **Nothing was written beside the deployed DLLs.**

### Second live check: the review fix batch (2026-11-01, 12:57-13:00)

The same setup: a CEM_RibbonUI Debug R24 deploy, the temporary starter, and new families from `CMGL-GenericModel`.
- **SEC-2:**
  - Revit 2024 accepts a view named `a/../../x`. Its own `ImageExportOptions.GetFileName` already sanitises the name to
    ` - Alzado - a-------x`, so the escape did not reproduce on this version.
  - `export_view_image` still replaced the name inside its rolled-back group, with the warning. The PNG landed in
    `images\`, and no PNG appeared anywhere else under `%TEMP%\Cemengal`.
  - Afterwards the view had its own name and detail level again.
- **QA-2:** two new families were loaded into family A.
  - `familyName` with `hostDocument: A` opened the first, which became active.
  - A second open by name **without** `hostDocument` failed clearly: "not found in 'Familia2' … name it with
    hostDocument". With `hostDocument` it opened.
  - The project default is not driven live: no project was opened, because the allowed models are IFCs.
- **QA-5:** each copy got its own `Families\<name>_<stamp>\` folder. The fixed-name copy from the first check was not
  overwritten.
- **Reuse by path:** `familyPath` of the open A reused that document and activated it.
- **The rest:** nothing was written beside the deployed DLLs. Revit closed with one "No" to the save prompt, and the
  starter was removed.

**Starting it without a click** (used that day): a throwaway starter add-in, a temporary `.addin` deleted afterwards.
It raises an `ExternalEvent` on the first `Idling`, which:
1. makes the test families and activates one;
2. posts `CustomCtrl_%CustomCtrl_%Cemengal%CEM_AI%CEM_IAModeler.Core.MCPServiceConnection`.

Two things to expect:
- **A redeploy re-prompts.** Revit asks "Seguridad - Complemento sin firma" again for every redeployed Cemengal add-in.
  "Cargar una vez" answers it without storing trust.
- **The notice's title** is `CEM_RibbonUI - revitMCP` ("Open Server", button `&Cerrar`).

Revit's "Guardar archivo" prompt takes `BM_CLICK` on its `&No` button. UI Automation finds it but cannot invoke it.

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
| `take_screenshot`: "Controlador no válido" / invalid handle; ribbon clicks and UIA Invoke do nothing | the desktop is not rendering (e.g. a minimized RDP client). GDI capture cannot work, so record the screenshot as not proven live. To start the server without a click, use a throwaway starter add-in (one temporary `.addin`, deleted afterwards) whose first `Idling` with a document calls `UIApplication.PostCommand(RevitCommandId.LookupCommandId("CustomCtrl_%CustomCtrl_%Cemengal%CEM_AI%CEM_IAModeler.Core.MCPServiceConnection"))`. Journal playback does not work: it answers the unsigned-add-in prompts "No cargar" and rejects journaled answers. Native dialogs (unsigned add-in, central copied, "Open Server") accept `BM_CLICK` on their button handle (2026-10-24) |
| logs in `%TEMP%\<guid>\Cemengal\…` instead of `%TEMP%\Cemengal\RevitMCP` | Revit loaded a `CEM_CSharpExtended.dll` without `CemTempFolder.UserTempPath`; inside Revit the plain temp path is the per-session folder. The first copy loaded wins for every add-in, so redeploy all of them ([audit trails](../concepts/audit-trails.md)) |
