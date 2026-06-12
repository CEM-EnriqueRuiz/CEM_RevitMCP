# Revit MCP Toolset — Architecture, Conventions & Expansion Roadmap

Goal: grow this command set into a toolset broad and generic enough that Claude can act as a
**senior Revit modeler** — strong on MEP, Families, Architecture, Coordination, and Parameters.

This document is the contract for adding tools. Every new tool follows the same 4-part pattern and
the same conventions, so the AI sees a consistent, predictable surface.

---

## 1. The anatomy of one tool (4 parts, 3 names that must match)

A tool spans **two projects**. The glue is a single **command-name string** that must be identical
in three places.

```
        ┌────────────────────────── server/  (TypeScript, what the LLM sees) ───────────────┐
        │  src/tools/<name>.ts                                                               │
        │     server.tool("my_command", "<description>", { ...zod schema... }, handler)       │
        │     handler → revitClient.sendCommand("my_command", { data })                       │
        └─────────────────────────────────────────────────────────────────────────────────────┘
                                      │  TCP JSON-RPC, port 8080
                                      ▼
        ┌────────────────────────── commandset/  (C#, runs in Revit) ─────────────────────────┐
        │  Commands/MyCommand.cs        : ExternalEventCommandBase  — parse JObject, raise event│
        │  Services/MyEventHandler.cs   : IExternalEventHandler     — do Revit work on UI thread │
        │  Models/<Domain>/MyModels.cs  : payload + result DTOs (Newtonsoft [JsonProperty])      │
        └─────────────────────────────────────────────────────────────────────────────────────┘
                                      │  registered in
                                      ▼
        command.json  →  { "commandName": "my_command", "assemblyPath": "RevitMCPCommandSet.dll" }
```

The three names that MUST be byte-identical:
1. `server.tool("my_command", …)` in the `.ts`
2. `public override string CommandName => "my_command";` in the C# command
3. the `"commandName"` entry in `command.json`

> **Why two threads / the EventHandler dance.** The socket receives the AI request on a background
> thread; the Revit API only runs on the UI thread. `ExternalEventCommandBase` raises a Revit
> `ExternalEvent` and blocks until the `IExternalEventHandler.Execute(UIApplication)` runs on the UI
> thread and signals back. **Never call the Revit API directly from the command's `Execute` — always
> through the handler.**

---

## 2. Canonical templates (copy these)

### 2a. C# Command — `commandset/Commands/<Domain>/MyCommand.cs`

```csharp
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    public class MyCommand : ExternalEventCommandBase
    {
        private MyEventHandler _handler => (MyEventHandler)Handler;

        public override string CommandName => "my_command";

        public MyCommand(UIApplication uiApp)
            : base(new MyEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            // Array payload:   parameters["data"]?.ToObject<List<MyInfo>>()
            // Object payload:  parameters["data"]?.ToObject<MyInfo>()
            var data = parameters["data"]?.ToObject<List<MyInfo>>();
            if (data == null || data.Count == 0)
                throw new ArgumentNullException(nameof(data), "No data provided");

            _handler.SetParameters(data);

            if (_handler.WaitForCompletion(15000))      // ms; bump for heavy ops
                return _handler.Result;
            throw new TimeoutException("my_command timed out");
        }
    }
}
```

### 2b. C# EventHandler — `commandset/Services/MyEventHandler.cs`

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class MyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<MyInfo> Requests { get; private set; }
        public AIResult<List<MyResult>> Result { get; private set; }

        public void SetParameters(List<MyInfo> data) { Requests = data; _resetEvent.Reset(); }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;
            try
            {
                var results = new List<MyResult>();
                using (var tx = new Transaction(doc, "My Command"))
                {
                    tx.Start();
                    foreach (var req in Requests)
                    {
                        // ... per-item Revit work. NEVER hard-fail the batch:
                        // skip the bad item, record a warning in its result, continue.
                    }
                    tx.Commit();
                }

                int ok = results.Count(r => r.Success), fail = results.Count - ok;
                Result = new AIResult<List<MyResult>>
                {
                    Success = fail == 0 || ok > 0,
                    Message = $"{ok} succeeded, {fail} failed. Per-item details in Response.",
                    Response = results
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<MyResult>>
                    { Success = false, Message = $"Error in my_command: {ex.Message}" };
            }
            finally { _resetEvent.Set(); }
        }

        public bool WaitForCompletion(int timeoutMs = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMs);
        }

        public string GetName() => "My Command";
    }
}
```

### 2c. TypeScript tool — `server/src/tools/my_command.ts`

```ts
import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const myInfo = z.object({
  // mirror the C# model field-for-field; describe EVERY field — the LLM reads these
});

export function registerMyCommandTool(server: McpServer) {
  server.tool(
    "my_command",
    "<one sentence on what it does + 'All units in mm/deg.' + what it returns>",
    { data: z.array(myInfo).min(1).describe("…") },   // or just `data: myInfo` for object payloads
    async (args) => {
      const params = { data: args.data };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("my_command", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text",
          text: `my_command failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
```

The exported function **must** start with `register` — `tools/register.ts` auto-discovers every
`*.ts` in the folder and calls its `register*` export. No manual wiring needed.

### 2d. Register in `command.json`

```json
{ "commandName": "my_command", "description": "…", "assemblyPath": "RevitMCPCommandSet.dll" }
```

`command.json` is copied to `Commands/commandRegistry.json` by CEM_RibbonUI's build
(`StageRevitMcpCommandSet` target). Keep `command.json` the single source of truth.

---

## 3. Conventions (non-negotiable — these make tools "AI-grade")

1. **mm in, mm out.** Length/Area/Volume in mm / mm² / mm³, angles in degrees. Convert to Revit
   internal units (feet/radians) only at the boundary, inside the handler. Use `McpParameterUtils`
   / `ElementIdExtensions` helpers; don't open-code conversions.
2. **Return `AIResult<T>` with a rich `Message`.** Include a human-readable summary and a
   `⚠ Warnings` section. The LLM reads `Message` and self-corrects — a good message is half the tool.
3. **Never hard-fail on partially bad input.** Skip the bad item, warn, continue. Generic tools get
   messy AI input; a batch of 50 should not die on item 7.
4. **Accept both display names and enum names** (`BuiltInParameter`, `BuiltInCategory`). The model
   doesn't always know which it has. Resolve with a helper that tries both.
5. **Idempotent where possible.** Reuse existing filters/parameters/types/definitions by name or
   GUID instead of erroring on "already exists".
6. **One Command + one EventHandler per tool.** Handler owns its own `Transaction`.
7. **Multi-version safe (Revit 2020–2026).** Guard version-specific API with the existing
   `REVIT2022_OR_GREATER` / `REVIT2023_OR_GREATER` / `REVIT2024_OR_GREATER` defines. `ElementId`
   uses `long` from 2024 — use the existing `ElementIdExtensions` instead of `new ElementId(int)`.
8. **Name carefully.** `verb_noun`, lowercase snake_case: `create_*`, `get_*`, `set_*`, `modify_*`,
   `delete_*`, `tag_*`, `place_*`, `duplicate_*`, `analyze_*`.

---

## 4. Current toolset (33 commands)

**Access/Query:** get_available_family_types, get_current_view_elements, get_current_view_info,
get_selected_elements, analyze_model_statistics, export_room_data, get_material_quantities,
ai_element_filter, operate_element
**Create/Modify:** create_point_based_element, create_line_based_element,
create_surface_based_element, create_grid, create_level, create_room,
create_structural_framing_system, color_splash, tag_walls, tag_rooms, create_dimensions,
delete_element, send_code_to_revit
**Parameters/Views (the 5 just added):** create_reference_plane, create_shared_parameter,
set_element_parameters, create_view_filter, create_global_parameter

---

## 5. Expansion roadmap (the "senior modeler" surface)

Ordered roughly by leverage. Each is one tool unless noted. ✦ = needs a new Model DTO group.

### 5.1 Parameters & data backbone  *(do first — everything else leans on it)*
- [ ] `get_element_parameters` — full read-back companion to `set_element_parameters` (names, values,
      storage types, units, is-type). The single most useful tool for a modeler AI.
- [ ] `create_project_parameter` ✦ — shared-param variant bound via `ParameterBindings` without the
      shared file (project params).
- [ ] `duplicate_type` ✦ — generic `ElementType.Duplicate` + parameter overrides (drives families/MEP types).
- [ ] `transform_elements` ✦ — move / rotate / mirror / array / copy a batch (ElementTransformUtils).
- [ ] `get_categories` / `list_parameters_for_category` — discovery so the AI learns what's writable.

### 5.2 MEP  *(primary focus)*
- [ ] `create_mep_system` ✦ — ducts/pipes/conduit/cable tray by routing points + type + size (mm).
- [ ] `connect_mep_elements` ✦ — connect by `Connector`, auto-route fittings.
- [ ] `place_mep_equipment` ✦ — FamilyInstance on level/face with system + connector hookup.
- [ ] `set_mep_sizes` — width/height/diameter/insulation as a typed parameter facade.
- [ ] `create_mep_zone` / `space` ✦ — spaces, zones, space separation lines.
- [ ] `get_mep_system_info` — traverse a system, return topology + flow/sizing for review.
- [ ] `tag_mep_elements` — generic tag-by-category-in-view (generalize tag_walls/tag_rooms).

### 5.3 Families  *(primary focus)*
- [ ] `load_family` ✦ — load `.rfa` from path/library; optional overwrite + type selection.
- [ ] `place_family_instance` ✦ — generic placement (point/line/face/host/work-plane), level, rotation.
- [ ] `create_family_type` — new type in a loaded family by duplicating + overriding params.
- [ ] `get_family_types` — list types + their parameters (extends get_available_family_types).
- [ ] **Family editor** (separate context — only valid in a family doc):
      - [ ] `associate_family_parameter` ✦ — `FamilyManager.AssociateElementParameterToFamilyParameter`.
      - [ ] `add_family_parameter`, `set_family_parameter_formula`, `add_family_type`.
      > Gate these behind a `doc.IsFamilyDocument` check; report a clear error otherwise.

### 5.4 Architecture
- [ ] `create_wall` / `create_floor` / `create_ceiling` / `create_roof` ✦ — typed, by curve/profile (mm).
- [ ] `create_opening` ✦ — by face / shaft / wall.
- [ ] `create_stairs` / `create_railing` ✦.
- [ ] `modify_room` — boundaries, names, numbers, params (companion to create_room).
- [ ] `create_area_plan` / `create_area_boundary` ✦.

### 5.5 Views, Sheets & Documentation
- [ ] `create_view` ✦ — floor/ceiling/section/elevation/3D/drafting (Models/Views already exists).
- [ ] `create_sheet` + `place_viewport` ✦ — sheet from titleblock, place views at scale.
- [ ] `create_schedule` ✦ — by category + fields + filters/sorting (Models/Views exists).
- [ ] `duplicate_view` (+ as dependent / with detailing), `set_view_template`.
- [ ] `set_view_range` / `crop` / `scale`.
- [ ] `place_legend`, `create_legend_component`.

### 5.6 Coordination & Management  *(primary focus)*
- [ ] `manage_worksets` ✦ — create/list worksets, assign elements.
- [ ] `link_model` / `manage_links` ✦ — RVT/IFC/CAD link load/unload/reload, set workset.
- [ ] `copy_monitor` ✦ — set up Copy/Monitor relationships (levels/grids/MEP).
- [ ] `run_interference_check` ✦ — `BimInterferenceCheck` between category sets → clash report JSON.
- [ ] `get_warnings` — `doc.GetWarnings()` summarized + grouped (huge for QA).
- [ ] `purge_unused` ✦, `audit_model_health` — counts, groups, in-place families, naming issues.
- [ ] `phase_management` ✦ — phases + phase filters; set element phase created/demolished.
- [ ] `manage_design_options` ✦.

### 5.7 Geometry / Reference
- [ ] `create_grid` (have) → add `create_multi_segment_grid`, `create_scope_box`.
- [ ] `join_geometry` / `unjoin`, `switch_join_order`.
- [ ] `create_assembly` ✦, `create_group` / `manage_groups` ✦.

---

## 6. Suggested folder structure as it grows

Mirror domains in both projects so the surface stays navigable:

```
commandset/Commands/{MEP,Families,Architecture,Views,Coordination,Parameters,Geometry}/…
commandset/Services/…            (flat or same domain subfolders)
commandset/Models/{MEP,Architecture,Views,Coordination,Common}/…
server/src/tools/…               (flat; names are self-grouping by prefix)
```

`McpParameterUtils` is the shared helper hub (spec/unit/parameter/category/enum resolution).
Add to it rather than re-implementing resolution per handler.

---

## 7. Build & deploy checklist (per batch of new tools)

1. C# command set builds: `dotnet build commandset\RevitMCPCommandSet.csproj -c "Debug R24" -p:Platform=x64`
2. Add entries to `command.json`.
3. Add `server/src/tools/<name>.ts`; `cd server && npm run build`.
4. Rebuild CEM_RibbonUI (**with Revit closed** so the Addins folder isn't locked):
   `dotnet build CEM_RibbonUI\CEM_RibbonUI.csproj -c "Debug R24" -p:Platform=x64`
   → stages `Commands\` (DLL + deps + commandRegistry.json) next to RevitMCPPlugin.dll and deploys.
5. In Revit: click **CEM_RevitAI** to start the server; point your MCP client (Claude) at the
   `server/build/index.js` stdio server.
6. Smoke-test the new tool from Claude; read the `AIResult.Message` to confirm.

> If you build while Revit is open you'll get `MSB3231 … Access to the path … is denied` on a locked
> DLL — that's the deploy step, not a code error. Build with `-p:DeployRevitAddin=false` to compile
> without deploying, or close Revit to deploy.
