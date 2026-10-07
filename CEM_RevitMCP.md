# CEM_RevitMCP — Architecture, Conventions, Toolset & Roadmap

The single source of truth for this repository: how the Revit MCP tool set is structured, how to add
tools, what exists today, and what is deliberately deferred. (User-facing install lives in `README.md`;
the AI-behaviour guide is the `cem-aimodeler` skill, `.claude/skills/cem-aimodeler/SKILL.md`.)

Goal: grow this command set into a tool surface broad and generic enough that Claude can act as a
**senior Revit modeler** — strong on Architecture, MEP, Families, Structure, Coordination and Parameters,
with eyes (`take_screenshot`) to evaluate and iterate.

> **Authoritative tool count = entries in `command.json`** (each has a matching C# `CommandName` and a
> TS `server.tool`). Today: **120 entries in `command.json`, but the server exposes 118 MCP tools.**
> The gap is real, not a miscount: `tag_walls` and `tag_rooms` were consolidated into the single
> `tag_elements` tool, and `command.json` still carries their old names. The C# side still serves
> both command names; they are simply no longer separate MCP tools. To regenerate the list:
> `grep -o '"commandName"[^,]*' command.json`.

---

## 1. The two projects + the socket chain

```
Claude (MCP client) ─► local TS server (server/build/index.js) ─► TCP :8080 ─► RevitMCPPlugin ─► RevitMCPCommandSet (the 120 commands)
```

- **`server/`** (TypeScript) — what the LLM sees. One file per tool in `server/src/tools/`, auto-discovered
  by `register.ts` (any exported function whose name starts with `register*`). No manual wiring.
- **`commandset/`** (C#, runs in Revit) — Command + EventHandler + Model DTOs per tool. One monolithic
  `RevitMCPCommandSet.dll` per Revit version (2020–2026).
- **`plugin/`** (C#) — the socket service, command registry/dispatch (`CommandExecutor`), external-event
  plumbing. Commands resolve **purely by `CommandName` string** (reflection in `CommandManager`), so
  folders and namespaces are organisational only — runtime never depends on them.
- **`command.json`** (root) — the registry copied to `Commands/commandRegistry.json` by the CEM_RibbonUI
  build. Single source of truth for which commands exist.

### Audit trails (on disk, next to the model work)
Two fail-soft, timestamped logs under `…\05-CEMAIModeler\` (hardcoded today; TODO make configurable):
- **`Screenshots\`** — optional PNG per `take_screenshot`.
- **`Log\`** — JSONL one line per *every* tool call (`plugin/Utils/ActionLogger.cs`, written at
  `CommandExecutor`): `ts, tool, input, ok, result`. `send_code_to_revit` bodies are omitted — the C#
  is deliberately not persisted anywhere (a per-call `Recipes\` dump was removed as noise). This is
  how a whole AI session is evaluated after the fact.

---

## 2. Domains = API namespaces (the empirical map)

Tool **domains are derived from the Revit 2024 API namespaces**, not invented. Source inventories:
`commandset/Utils/RevitAPI_2024_namespaces.md` and `commandset/Utils/RevitAPI_2024_classes_by_namespace.txt`
(extracted from `commandset/Utils/2024.chm`). Each API namespace = a candidate tool domain.

| Namespace | What lives here | Pack |
|---|---|---|
| `Autodesk.Revit.DB` (1078 classes, 151 create methods) | Cross-cutting core: Wall, Floor, Ceiling, Roof, Grid, Level, ReferencePlane, FamilyInstance, Views (Plan/Section/3D/Drafting/Sheet/Schedule), Viewport, Dimension, TextNote, IndependentTag, FilledRegion, Group, Assembly, Phase, Material, CurtainGrid | **Core** (flat names) |
| `Autodesk.Revit.DB.Architecture` | Room, RoomTag, Stairs/Railing, BuildingPad, Topography, Fascia/Gutter (+ DB host elements Wall/Floor/Ceiling/Roof/Opening) | `arch_*` |
| `DB.Mechanical` + `DB.Plumbing` + `DB.Electrical` | Duct/Pipe/Conduit/CableTray, systems, equipment, spaces | `mep_*` |
| `DB` family API (`Family`, `FamilySymbol`, `FamilyManager`, `FamilyItemFactory`) | Loadable + family-editor geometry/params | `family_*` |
| `Autodesk.Revit.DB.Structure` | Framing/columns/footings (rebar/loads/analytical excluded — fragile) | `struct_*` |
| worksharing / links / QA (+ `DB.IFC`) | worksets, links, phases, purge, audit | `coord_*` |
| `Autodesk.Revit.DB.Visual` | material appearance | `viz_*` |

**Key truth:** views, sheets, schedules, tags, dimensions, text, filled regions, line/fill patterns,
object styles ALL live in `Autodesk.Revit.DB` → they **extend Core with flat names**, they are NOT separate
domains. The only real "appearance" namespace is `DB.Visual` → `viz_*`.

### Naming convention (the persona-filter key)
- **Core tools:** flat `verb_noun` — `set_element_parameters`, `create_view`, `tag_elements`.
- **Domain tools:** `domain_verb_noun` — `arch_create_wall`, `mep_create_duct`, `family_load`,
  `struct_create_beam`, `coord_link_model`, `viz_create_material`, `parent_*`.
- **The prefix == the pack == the folder == the namespace == the future persona-filter key.** A persona is
  just a system prompt + a tool subset selected by prefix (no C# changes); deferred for now (all tools exposed).

---

## 3. Repository folder structure (mirror domains across all three trees)

Commands, Services and Models each mirror the same domain subfolders. **Folder name == namespace suffix ==
the `domain_` tool prefix** (so navigation, code and persona-filtering all agree):

```
commandset/Commands/{Core, Access, Architecture, Mep, Family, Struct, Views, AnnotationComponents,
                     DataExtraction, Delete, Test, ExecuteDynamicCode, ParentTools}/
commandset/Services/{ … same domains … }/
commandset/Models/{Common, Architecture, Mep, Family, Struct, Views, AnnotationComponents, DataExtraction}/
server/src/tools/                 (flat; names self-group by prefix)
```

- `Core` is the catch-all flat-name pack (also currently holds the `coord_*`, `viz_*`, style and DB-gap
  tools — they are flat-named DB/links/visual tools, kept here by convention).
- `Common` (Models only) holds shared DTOs (`AIResult<T>`, `JZPoint`, `ParamOverride`) and the cross-cutting
  request/result models (ParentTools, Struct members, Coord, Viz, Style, DbGap, Screenshot).
- `ParentTools` = high-level **pipeline** tools (not single-API wrappers): `parent_link_extract_geometry`,
  `parent_solid_to_member_params`, `parent_reconstruct_native`, `parent_validate_deviation` — they collapse
  the linked-model(IFC) → native reconstruction workflow the AI used to hand-write as recipes.

`commandset/Utils/` is the shared helper hub — extend it rather than re-implementing resolution per handler:
`McpResolveUtils` (units/level/category/type/curve-loop), `McpParameterUtils`, `McpFamilyUtils`
(`OverwriteFamilyLoadOptions`, structural-type parse), `JZPoint.ToXYZ` (mm→ft), `ElementIdExtensions.GetValue`.

> **History note:** earlier the trees drifted (`Models/MEP` vs `Services/Mep`, `Structure` vs `Struct`,
> `Annotation` vs `AnnotationComponents`). Aligned 2026-06-14 to the prefix-matching names above.

---

## 4. The anatomy of one tool (4 parts, 3 names that must match byte-for-byte)

```
server/src/tools/<name>.ts        server.tool("my_command", desc, zodSchema, handler)
                                  handler → withRevitConnection(c => c.sendCommand("my_command", params))
        │  TCP JSON-RPC :8080
        ▼
commandset/Commands/<Domain>/MyCommand.cs    : ExternalEventCommandBase — parse JObject, raise event, wait
commandset/Services/<Domain>/MyEventHandler.cs: IExternalEventHandler   — do Revit work on the UI thread
commandset/Models/<Domain>/MyModels.cs        : request/result DTOs ([JsonProperty])
        │  registered in
        ▼
command.json  →  { "commandName":"my_command", "description":"…", "assemblyPath":"RevitMCPCommandSet.dll" }
```

The three names that MUST be identical: the TS `server.tool("name")`, the C# `CommandName => "name"`,
and the `command.json` `commandName`.

> **Why the EventHandler dance:** the socket receives the request on a background thread; the Revit API only
> runs on the UI thread. `ExternalEventCommandBase` raises a Revit `ExternalEvent` and blocks until
> `IExternalEventHandler.Execute(UIApplication)` runs on the UI thread and signals back. **Never call the
> Revit API directly from the command's `Execute` — always through the handler.**

### Canonical templates

**Command** (compact modern style, see `Commands/Struct/StructCommands.cs`, `Commands/ParentTools/…`):
```csharp
public class MyCommand : ExternalEventCommandBase
{
    private MyEventHandler H => (MyEventHandler)Handler;
    public override string CommandName => "my_command";
    public MyCommand(UIApplication u) : base(new MyEventHandler(), u) { }
    public override object Execute(JObject p, string r)
    {
        var d = p?.ToObject<MyRequest>();
        if (d == null) throw new ArgumentException("body required");
        H.Request = d;
        if (RaiseAndWaitForCompletion(20000)) return H.Result;   // ms; 60–300s for heavy/geometry ops
        throw new TimeoutException("my_command timed out");
    }
}
```

**EventHandler** — `IExternalEventHandler, IWaitableExternalEventHandler`; owns its own `Transaction`;
returns `AIResult<T>` with a rich `Message`; signals `_resetEvent.Set()` in `finally`.

**TS tool** — zod schema mirrors the C# DTO field-for-field (describe **every** field — the LLM reads them);
`sendCommand("name", params)`; exported `register*` function.

### Conventions (non-negotiable — these make tools "AI-grade")
1. **mm in, mm out** (lengths mm, angles deg). Convert at the boundary via `JZPoint.ToXYZ` / `McpResolveUtils.MmToFt`.
2. **Return `AIResult<T>` with a human-readable `Message`** incl. a `⚠ Warnings` summary — the LLM reads it to self-correct.
3. **Never hard-fail a batch.** Skip the bad item, warn, continue.
4. **Accept display names AND enum names** (`BuiltInParameter`, `BuiltInCategory`).
5. **Idempotent where possible** — reuse existing filters/types/params by name; don't error on "already exists".
6. **One Command + one EventHandler per tool.** Handler owns its `Transaction` (the **family doc**, not the project doc, for family geometry).
7. **Multi-version safe (2020–2026)** — guard with `REVIT2022_OR_GREATER` etc.; use `ElementIdExtensions`, not `new ElementId(int)`.
8. **`verb_noun` snake_case**, domain-prefixed for non-core.

### Build & deploy checklist (per batch)
1. `dotnet build commandset\RevitMCPCommandSet.csproj -c "Debug R24" -p:Platform=x64` (`-p:DeployRevitAddin=false` to compile without deploying).
2. Add `command.json` entries; add `server/src/tools/<name>.ts`; `cd server && npm run build`.
3. Rebuild CEM_RibbonUI **with Revit closed** (it stages `Commands\` + `commandRegistry.json` next to the plugin and deploys). A locked-DLL `MSB3231` here = the deploy step, not a code error.
4. In Revit click **CEM_RevitAI** to start the server; point the MCP client at `server/build/index.js`.
5. Smoke-test from Claude; read the `AIResult.Message`.

---

## 5. Current toolset (120 wired commands)

**Access/Query:** get_available_family_types, get_current_view_elements, get_current_view_info,
get_selected_elements, find_elements, get_element_info, get_element_geometry, get_element_parameters,
analyze_model_statistics, export_room_data, get_material_quantities, get_warnings, ai_element_filter, operate_element
**Core/Edit:** set_element_parameters, copy_parameter_values, bulk_set_by_filter, list_parameters_for_category,
transform_elements, delete_element, delete_elements, group_elements, ungroup_group, pin_elements, rename_element,
duplicate_type, send_code_to_revit
**Parameters:** create_shared_parameter, create_project_parameter, create_global_parameter, create_reference_plane
**Views/Docs/Annotation:** create_view, create_sheet, place_viewport, create_view_filter, create_dimensions,
tag_elements, tag_walls, tag_rooms, color_splash, create_schedule, duplicate_view, set_view_properties,
apply_filter_to_view, place_text, create_filled_region, create_revision, create_legend, **take_screenshot**
**Styles (DB graphics):** manage_line_pattern, manage_fill_pattern, set_object_styles
**Materials/Appearance (viz_*):** viz_create_material, viz_set_material_appearance, viz_list_materials, viz_assign_material
**Coordination/QA (coord_*):** coord_manage_worksets, coord_link_model, coord_purge_unused, coord_audit_model, coord_manage_phases
**DB-gap fills:** create_model_lines, create_detail_lines, create_direct_shape, create_spot_dimension,
create_assembly, create_multi_segment_grid, edit_curtain_grid, place_image, create_parts
**Geometry base:** create_grid, create_level, create_room, create_point_based_element, create_line_based_element,
create_surface_based_element, create_structural_framing_system
**MEP (7):** mep_create_duct, mep_create_pipe, mep_create_conduit, mep_create_cable_tray, mep_place_equipment,
mep_create_space, mep_get_system_info
**Architecture (13):** arch_create_wall, arch_create_floor, arch_create_ceiling, arch_create_roof,
arch_create_curtain_wall, arch_create_opening, arch_create_room, arch_create_stairs, arch_create_railing,
arch_create_area, arch_create_area_plan, arch_create_separator, arch_join_geometry, arch_unjoin_geometry
**Family (13):** family_load, family_place_instance, family_get_types, family_create_type, family_add_parameter,
family_add_type, family_set_formula, **family_open_session**, **family_save_session**, family_create_extrusion
(multi-loop holes + arcs + arbitrary planes + material), family_create_revolution, family_associate_parameter,
family_add_reference_plane
**Structure (struct_*, 4):** struct_create_beam, struct_create_brace, struct_create_column, struct_create_foundation
**ParentTools (parent_*, 4):** parent_link_extract_geometry, parent_solid_to_member_params,
parent_reconstruct_native, parent_validate_deviation

**Removed (were TS-only local SQLite, never touched Revit):** store_project_data, store_room_data,
query_stored_data + the `src/database/` layer. Dropped along with the `better-sqlite3` native dependency so the
server bundles to a single cross-platform file (`npm run bundle` -> `dist/index.js`); the cache was a per-machine
scratchpad that nothing else used.

> Naming note: a TS tool targeting a Revit command must use the exact `command.json` name (e.g. `color_splash`,
> not `color_elements`). Legacy `tag_all_walls`/`tag_all_rooms` were removed — use `tag_elements` with categories.

---

## 6. `Autodesk.Revit.DB` coverage statement (the "senior-modeler complete" bar)

Scope decision: target = **create methods + common edits**, NOT full read/write parity. The CHM's "16,084
DB topics" are doc pages (every overload/property/enum/event), not actionable ops; the real surface is
~151 DB create methods + the writable element families. Reads stay generic via
`get_element_info` / `get_element_parameters` / `get_element_geometry` / `find_elements`.

**Covered:** the high-frequency build surface + P1 (6 tools: model/detail lines, direct shape, spot dim,
assembly, multi-segment grid) + P2 (3 tools: curtain-grid edit, image, parts). DB is "senior-modeler
complete" for create+edit at the family level.

**Deliberately deferred (documented, not forgotten):**
- ExtrusionRoof (would change `arch_create_roof` schema — add later as a `kind` flag), ModelText (family-doc only),
  wall/host sweeps (needs `WallSweepInfo`, fragile), BeamSystem/WallFoundation (→ future struct work).
- Long tail → `send_code_to_revit`: conceptual mass (Form/Blend/Sweep/DividedSurface), adaptive components,
  site (Topography/BuildingPad/PropertyLine), AreaScheme, sun/shadow.
- **No public API (cannot build):** scope box (only `VolumeOfInterest*` properties exist).
- Rebar / loads / analytical: fragile + version-sensitive (analytical reworked in 2023+) → `send_code_to_revit`.
- `NewBlend` for families: fights the API (needs profiles on different planes) → decompose into sloped extrusions.

**NOT pursued:** full property-level parity — past ~100 undifferentiated tools, LLM tool-selection degrades.

---

## 7. Build order — status (all planned packs complete)

1. ✅ **Core** — params, transform, group/pin/rename, views/sheets/viewports, filters, tags, warnings, queries.
2. ✅ **MEP** (7) · 3. ✅ **Family** (project + editor) · 4. ✅ **Architecture** (13).
5. ✅ **Core DB extensions** — schedules, view props, filters-to-view, legend, text, filled region, revision, line/fill patterns, object styles.
6. ✅ **viz_\*** (4, `DB.Visual`) · 7. ✅ **coord_\*** (5) · 8. ✅ **struct_\*** (4).
9. ✅ **DB-gap fills** (P1+P2, 9) · 10. ✅ **ParentTools** (4, IFC→native pipeline).
11. ✅ **Family-edit session + geometry upgrade (2026-06-14):** `family_open_session`, `family_save_session`,
    and `family_create_extrusion` upgraded (multi-loop holes, line+arc segments, arbitrary {normal,origin}
    planes, material) — closed the gap that forced 134/198 of that day's recipes into `send_code_to_revit`.

**Persona delivery:** deferred — keep all tools exposed; revisit env-var prefix-filtering once the surface
warrants it (LLM tool-selection degrades with very large undifferentiated lists).

---

## 8. Smoke test (verify the full chain end-to-end)

**Prereqs:** Revit 2024 + a scratch **project** (and optionally an **.rfa**); `server/build/index.js` built
(`cd server && npm install && npm run build`).

**Point the MCP client at the LOCAL build** (the published npm package lacks new tools):
```json
{ "mcpServers": { "cem-revit-ai": {
  "command": "node",
  "args": ["d:\\repos\\Cemengal\\CEM_IA\\CEM_RevitMCP\\mcp-servers-for-revit\\server\\build\\index.js"] } } }
```
(Claude Code CLI: `claude mcp add cem-revit-ai -- node "…\server\build\index.js"`.)

**Start Revit side:** open a project → Cemengal ribbon → **CEM_RevitAI** (says "Open Server" → listens on :8080
and loads the background commands). Leave the project active.

**Run, in order** (read-only first):
- A. `get_warnings`; `family_get_types` (OST_Doors); `get_element_parameters` of a selected element.
- B. `create_view` (FloorPlan); `arch_create_wall` (0,0,0)→(5000,0,0) h3000; `transform_elements` copy it; `mep_create_pipe` (needs pipe+system types).
- C. Family: `family_open_session` an .rfa → `family_create_extrusion` (try a hollow profile on a sloped `planeDef` + a `material`) → `family_save_session(save, loadIntoProject)`. In a project doc the editor-only tools should fail clearly.

**Pass = ** read-only returns data; creation returns `Success:true` + ids that actually appear in Revit;
family-editor tools succeed in an .rfa and fail clearly in a project. Afterwards the `Log\actions_*.jsonl`
should show `family_*`/`arch_*` calls rather than `send_code_to_revit`.

**Triage:** "connect to revit client failed" → server not started / wrong port. Tools missing → client on
npm package or stale build (use local `build/index.js`, `npm run build`). "Command not found" → registry not
deployed / name mismatch. "No <X>Type available" → project lacks that type. 0 commands loaded → rebuild
CEM_RibbonUI **with Revit closed**.
