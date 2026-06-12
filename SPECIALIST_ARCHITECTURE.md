# Revit MCP — Specialist Agents over a Shared Tool Core

This is the design for turning the command set into **several specialist personas** — Architectural
Senior Modeler, MEP Modeler, Family Creation Specialist (and more) — that all stand on one shared
base of common tools.

**Domains are derived empirically from the Revit 2024 API namespaces** (`commandset/Utils/2024.chm`),
not invented. Each API namespace = a candidate tool domain; we then assign domains to specialists
(§5 mapping table — deliberately left open to decide). Class inventories and counts below were
extracted from that CHM (see §6).

---

## 0. Domains = API namespaces (the empirical map)

Topic/class/create counts harvested from the 2024 CHM (`D:\tmp\revit2024_ns.tsv`). "create" = methods
named `Create*` or `New*` (the actionable build surface). Full class lists per namespace:
`D:\tmp\revit2024_classes_by_namespace.txt`.

| Namespace (candidate domain)            | topics | classes | create | What lives here |
|------------------------------------------|-------:|--------:|-------:|---|
| `Autodesk.Revit.DB`                      | 16084  |  1078   |  151   | Cross-cutting core: Wall, Floor, Ceiling, Roof, Grid, Level, ReferencePlane, FamilyInstance/Symbol, Views (Plan/Section/3D/Drafting/Sheet/Schedule), Viewport, Dimension, TextNote, IndependentTag, FilledRegion, Group, Assembly, Phase, Material, CurtainGrid/Mullion/Panel |
| `Autodesk.Revit.DB.Structure`            |  1411  |   119   |   53   | Rebar* (huge), PathReinforcement, FabricArea/Sheet, Loads (Point/Line/Area), LoadCase/Combination, Analytical* (members/panels/links), structural connections |
| `Autodesk.Revit.DB.Electrical`           |   711  |    75   |   14   | ElectricalSystem, Wire, CableTray, Conduit, panels/circuits, lighting/power |
| `Autodesk.Revit.DB.Analysis`             |   658  |    52   |    8   | Energy/MEP analysis, analytical surfaces, results, spatial field |
| `Autodesk.Revit.DB.Visual`               |   596  |    54   |    0   | Appearance assets / materials rendering (read-write of material appearance) |
| `Autodesk.Revit.UI`                      |   539  |    63   |    3   | TaskDialog, RibbonItem, UIDocument, prompts (mostly host plumbing, not model tools) |
| `Autodesk.Revit.DB.IFC`                  |   441  |    32   |    6   | IFC export/import settings, IFC entity mapping |
| `Autodesk.Revit.DB.Mechanical`           |   431  |    46   |   16   | **MEP-HVAC:** Duct, FlexDuct, DuctType, DuctInsulation/Lining, MechanicalSystem(Type), MechanicalEquipment, Space, Zone, SpaceTag |
| `Autodesk.Revit.DB.Architecture`         |   392  |    39   |   10   | **Arch:** Room, RoomTag, Stairs/StairsRun/Landing, Railing/HandRail/TopRail, BuildingPad, TopographySurface, Fascia/Gutter |
| `Autodesk.Revit.DB.Structure.StructuralSections` | 270 | 52 |   0   | Structural section shape geometry (I/L/C/HSS profiles) |
| `Autodesk.Revit.DB.Plumbing`             |   220  |    21   |   13   | **MEP-Piping:** Pipe, FlexPipe, PipeType, PipeInsulation, PipingSystem(Type), PipeSegment, FluidType |
| `Autodesk.Revit.Creation`                |   184  |     6   |  161   | The `Document.Create.NewX` / `Application.Create.NewX` factory hub — used by ALL domains (legacy creation entry points) |
| `Autodesk.Revit.ApplicationServices`     |   181  |     2   |    4   | Application, Document open/create, options |
| `Autodesk.Revit.DB.DirectContext3D`      |   180  |    28   |    0   | Custom 3D graphics overlays (advanced/rare) |
| `Autodesk.Revit.DB.Events`               |   176  |    60   |    4   | Document/app event args (hooks, not tools) |
| `Autodesk.Revit.DB.Lighting`             |   156  |    25   |    0   | Light source/group photometrics |
| `Autodesk.Revit.DB.ExtensibleStorage`    |    90  |     7   |    1   | Custom data schemas on elements (great for CEM metadata) |
| `Autodesk.Revit.DB.Fabrication`          |    74  |     7   |    0   | Fabrication parts/services (you already have Fabrication tools in CEM) |
| `Autodesk.Revit.DB.Infrastructure`       |    58  |     5   |    1   | Roads/bridges/alignments (Civil) |
| `Autodesk.Revit.DB.PointClouds`          |    95  |     9   |    0   | Point cloud instances/filters (scan-to-BIM) |
| `Autodesk.Revit.UI.Selection`            |    38  |     3   |    0   | PickObject/selection filters (interactive — limited use headless) |

Smaller/host namespaces (`Exceptions`, `Macros`, `ExternalService`, `UI.Events`, `Steel`, …) are
infrastructure, not tool domains.

This document is grounded in the **Revit 2024 API** (`commandset/Utils/2024.chm`). API signatures
cited were extracted from that CHM; when implementing, read the exact page (see §6 "Using the CHM").

---

## 1. The layering: Core + Domains, one server

Two orthogonal axes — keep them separate:

```
                         ┌──────────────────────────────────────────────┐
   PERSONA (prompt-side) │  Architect    MEP      Family     Coordinator │   <- WHO is acting
                         └───────┬──────────┬────────┬───────────┬───────┘
                                 │          │        │           │
                                 ▼          ▼        ▼           ▼
                         ┌──────────────────────────────────────────────┐
   TOOLS (server-side)   │            DOMAIN tool packs                  │   <- WHAT they can do
                         │  arch.*     mep.*     family.*    coord.*      │
                         ├──────────────────────────────────────────────┤
                         │                CORE tool pack                 │   <- shared by everyone
                         │  params, geometry, selection, views, query   │
                         └──────────────────────────────────────────────┘
```

- **Tools** are just the MCP commands we already build (the 4-part pattern in `TOOLSET_ROADMAP.md`).
  They don't know which persona called them. A persona is **not** a code construct on the C# side.
- **Personas** are a *presentation/governance* layer: a named subset of tools + a system prompt that
  says "you are a senior MEP modeler; prefer these tools; follow these conventions." Same server,
  same socket, same Revit plugin — different **tool exposure** and **instructions**.

Why this split: a senior modeler reuses 80% generic operations (set parameter, place family, create
view, tag, filter) and differs mainly in *domain knowledge* and a *thin slice* of specialized tools.
Duplicating the common 80% per specialist would be unmaintainable.

---

## 2. The CORE pack (shared by every specialist)

These are the "basic tools that are the common approach of all." Most already exist; the rest are the
highest-priority additions. Group them under a `core_` mental namespace (tool names stay flat).

| Capability | Tools (✓ = exists today) | Key API (from CHM) |
|---|---|---|
| **Parameters** | `set_element_parameters` ✓, `get_element_parameters`◻, `create_shared_parameter` ✓, `create_project_parameter`◻, `create_global_parameter` ✓ | `Parameter.Set`, `Definitions.Create`, `Category`, `ParameterBindings`, `GlobalParametersManager` |
| **Selection / query** | `get_selected_elements` ✓, `ai_element_filter` ✓, `get_current_view_elements` ✓, `get_available_family_types` ✓, `operate_element` ✓ (select/hide/isolate/color) | `FilteredElementCollector`, `Selection`, `ElementFilter` |
| **Geometry / transform** | `transform_elements`◻ (move/rotate/mirror/array/copy), `create_reference_plane` ✓, `create_grid` ✓, `create_level` ✓ | `ElementTransformUtils`, `ReferencePlane`, `Grid.Create`, `Level.Create` |
| **Views & docs** | `create_view`◻, `duplicate_view`◻, `create_sheet`◻ + `place_viewport`◻, `create_schedule`◻, `create_view_filter` ✓ | `ViewPlan.Create`, `ViewSection.Create`, `View3D.Create*`, `ViewSheet.Create`, `Viewport.Create`, `ViewSchedule.CreateSchedule`, `ParameterFilterElement.Create` |
| **Annotation** | `create_dimensions` ✓, `tag_elements`◻ (generalize tag_walls/tag_rooms), `place_text`◻ | `IndependentTag.Create`, `Dimension` via `NewDimension`, `TextNote.Create` |
| **Model / QA** | `analyze_model_statistics` ✓, `get_warnings`◻, `get_material_quantities` ✓, `export_room_data` ✓ | `Document.GetWarnings`, `FailureMessage`, schedules |
| **Escape hatch** | `send_code_to_revit` ✓ (arbitrary C# — the universal fallback) | `Document`, full API via Roslyn |

◻ = build next. The **four** that unlock the most for every persona, in order:
`get_element_parameters`, `transform_elements`, `create_view` (+ `create_sheet`/`place_viewport`),
`tag_elements`.

---

## 3. Domain packs (one per API namespace)

Each pack maps to an API namespace from §0. Tools build on Core. Signatures are the real 2024 API.
**Which specialist owns which pack is decided in §5 — packs are defined independently of personas.**

### 3.1 `arch_*` — domain `Autodesk.Revit.DB.Architecture` (+ host elements from `DB`)
Walls, floors, ceilings, roofs, openings, stairs/railings, rooms/areas.
Classes (from CHM): Room, RoomTag, Stairs/StairsRun/StairsLanding, Railing/HandRail/TopRail/ContinuousRail,
BuildingPad, TopographySurface, Fascia/Gutter. Host elements (Wall/Floor/Ceiling/Roof/Opening) live in `DB`.

| Tool | API anchor (Revit 2024) |
|---|---|
| `create_wall` | `Wall.Create(Document, Curve, ElementId wallTypeId, ElementId levelId, double height, double offset, bool flip, bool structural)` — also the profile/`IList<Curve>` overload |
| `create_floor` | `Floor.Create(Document, IList<CurveLoop>, ElementId floorTypeId, ElementId levelId)` (+ slope arrow overload) |
| `create_ceiling` | `Ceiling.Create(Document, IList<CurveLoop>, ElementId, ElementId)` |
| `create_roof` | `FootPrintRoof` via `Document.Create.NewFootPrintRoof`, or extrusion roof |
| `create_opening` | `Document.Create.NewOpening` (wall/host/face), shaft openings |
| `create_stairs` | `Stairs.Create` + `StairsRun.Create*`, `StairsLanding.Create*` (needs a stairs edit scope) |
| `create_railing` | `Railing.Create(Document, ElementId, ElementId, ElementId, RailingPlacementPosition)` |
| `modify_room` / `create_room` ✓ | `Document.Create.NewRoom`, `Room.Location`, boundary params |
| `create_area_plan` / `create_area_boundary` | `ViewPlan.Create` (AreaPlan), `Document.Create.NewAreaBoundaryLine` |

### 3.2 `mep_*` — domains `DB.Mechanical` + `DB.Plumbing` + `DB.Electrical`
Ducts, pipes, conduit, cable tray, systems, equipment, spaces. (Three namespaces, one specialist pack.)
Classes (from CHM): Mechanical → Duct, FlexDuct, DuctType, DuctInsulation/Lining, MechanicalSystem(Type),
MechanicalEquipment, Space, Zone, SpaceTag. Plumbing → Pipe, FlexPipe, PipeType, PipeInsulation,
PipingSystem(Type), PipeSegment, FluidType. Electrical → ElectricalSystem, Wire, CableTray, Conduit.

| Tool | API anchor (Revit 2024) |
|---|---|
| `create_duct` | `Duct.Create(Document, systemTypeId, ductTypeId, levelId, XYZ, XYZ)` and connector overloads |
| `create_pipe` | `Pipe.Create(Document, systemTypeId, pipeTypeId, levelId, XYZ, XYZ)` / connector overloads |
| `create_conduit` | `Conduit.Create(Document, conduitTypeId, XYZ, XYZ, levelId)` |
| `create_cable_tray` | `CableTray.Create(Document, cableTrayTypeId, XYZ, XYZ, levelId)` |
| `create_flex_duct/pipe` | `Document.Create.NewFlexDuct(IList<XYZ>, FlexDuctType)` / `NewFlexPipe(...)` |
| `connect_mep` | `Connector.ConnectTo`, `Document.Create.NewElbowFitting/NewTeeFitting/NewTransitionFitting`, `MEPCurve.ConnectorManager` |
| `place_mep_equipment` | `FamilyInstance` placement + connector hookup; `MechanicalSystem`/`PipingSystem`/`ElectricalSystem.Create(Document, IList<ElementId>, type)` |
| `create_space` / `create_zone` | `Document.Create.NewSpace`, `Zone`, space separators |
| `set_mep_sizes` | typed facade over Duct/Pipe size, insulation: `DuctInsulation.Create`, `PipeInsulation.Create` |
| `get_mep_system_info` | traverse `MEPSystem.Elements` / `ConnectorManager` → topology + flow |

> MEP gotcha to bake into handlers: most MEP creation needs valid **system type** + **routing
> preferences**; resolve sensible defaults and warn (don't fail) when the AI omits them.

### 3.3 `family_*` — domain `DB` family API (`Family`, `FamilySymbol`, `FamilyManager`, `FamilyItemFactory`)
Two **distinct contexts** — guard every family-editor tool with `doc.IsFamilyDocument`.

**Project context (loaded families):**
| Tool | API anchor |
|---|---|
| `load_family` | `Document.LoadFamily(path, out Family)` / `LoadFamilySymbol` (+ overwrite options) |
| `place_family_instance` | `Document.Create.NewFamilyInstance(...)` (point/level/face/host/work-plane overloads) |
| `create_family_type` | `FamilySymbol.Duplicate(name)` + parameter overrides |
| `get_family_types` | `Family.GetFamilySymbolIds`, symbol params |

**Family-editor context (inside an .rfa):**
| Tool | API anchor |
|---|---|
| `add_family_parameter` | `FamilyManager.AddParameter(...)`, `AddSharedParameter(...)` |
| `set_family_parameter_formula` | `FamilyManager.SetFormula` |
| `add_family_type` | `FamilyManager.NewType(name)` |
| `associate_family_parameter` | `FamilyManager.AssociateElementParameterToFamilyParameter` |
| `create_family_geometry` | `FamilyItemFactory` (`NewExtrusion`, `NewBlend`, `NewRevolution`, `NewSweep`) on `doc.FamilyCreate` |
| `add_family_reference_plane` | `doc.FamilyCreate.NewReferencePlane` |

### 3.4 `coord_*` — domain: worksharing/links/QA (spans `DB` + `DB.IFC`)
| Tool | API anchor |
|---|---|
| `manage_worksets` | `Workset.Create`, `WorksetTable`, `WorksharingUtils` |
| `link_model` | `RevitLinkType.Create(Document, ModelPath, RevitLinkOptions)` + `RevitLinkInstance.Create`; CAD/IFC links |
| `run_interference_check` | `BimInterferenceCheck` (or document API) → clash report JSON |
| `get_warnings` | `Document.GetWarnings()` grouped/summarized |
| `copy_monitor` | `CopyPasteOptions`, monitor relationships |
| `purge_unused` / `audit_model_health` | collectors + `Document.Delete` of unused types |
| `phase_management` | `Phase`, `PhaseFilter`, element phase params |

### 3.5 `struct_*` — domain `Autodesk.Revit.DB.Structure` (1411 topics — large)
Rebar & reinforcement, structural framing/columns/foundations, loads, analytical model.
Classes (from CHM): Rebar*, RebarBarType, PathReinforcement, FabricArea/FabricSheet, AreaLoad/LineLoad/
PointLoad (+Types), LoadCase/LoadCombination/LoadNature, AnalyticalMember/AnalyticalPanel/AnalyticalLink.
Framing/columns/footings are `FamilyInstance` placed structurally (`DB` + `Structure` enums).
| Tool | API anchor (Revit 2024) |
|---|---|
| `create_rebar` | `Rebar.CreateFromCurves(...)`, `Rebar.CreateFromRebarShape(...)` |
| `create_path_reinforcement` | `PathReinforcement.Create(Document, hostId, IList<Curve>, flip, typeId, ...)` |
| `create_structural_framing` ✓-ish | `NewFamilyInstance(... StructuralType.Beam)` (have `create_structural_framing_system`) |
| `create_structural_column` | `NewFamilyInstance(... StructuralType.Column)` |
| `create_load` | `PointLoad.Create / LineLoad.Create / AreaLoad.Create(Document, ...)` |
| `create_analytical_member` | `AnalyticalMember.Create(...)`, `AnalyticalPanel.Create(...)` |

### 3.6 Other namespaces available as packs later
`infra_*` (`DB.Infrastructure` — roads/bridges/alignments), `fab_*` (`DB.Fabrication` — you already
have CEM Fabrication tools to mirror), `viz_*` (`DB.Visual` — material appearance), `ifc_*`
(`DB.IFC` — export/import settings), `storage_*` (`DB.ExtensibleStorage` — attach CEM metadata to
elements), `pointcloud_*` (`DB.PointClouds` — scan-to-BIM), `analysis_*` (`DB.Analysis` — energy/MEP).

---

## 4. How personas are actually wired (no C# changes)

A persona = **tool subset + system prompt**. Three ways to deliver it, pick per how you run Claude:

1. **Separate MCP server entrypoints (cleanest).** Add npm scripts that register only a subset:
   `server/src/personas/mep.ts`, `architect.ts`, etc., each calling a filtered `registerTools`.
   The MCP client connects to the one you want. Same C# plugin underneath.
2. **One server, tool-list filtering by env var.** `REVIT_PERSONA=mep` → `register.ts` only loads
   `core_*` + `mep_*` tools. Single build, switch by launch config.
3. **One server exposes everything; persona lives only in the system prompt.** Simplest; relies on
   the prompt to steer tool choice. Good to start; add filtering later if the tool list gets noisy.

Recommended: start with **#3**, move to **#2** once you have >~40 tools (LLM tool-selection degrades
with very large undifferentiated lists).

Filtering is a pure **prefix match** thanks to the naming convention (§5): a persona = `core_` tools +
the prefixes for its assigned domains. e.g. MEP persona = load tools whose name has no domain prefix
(core) OR starts with `mep_`. `register.ts` can read `REVIT_PERSONA` and skip files whose prefix
isn't in that persona's allow-list. No per-tool metadata needed — the name carries the pack.

---

## 5. Domain → Specialist mapping  ⟵ **TO DECIDE**

Domains (§0/§3) are defined from the API. Specialists are assembled from `core` + chosen domain packs.
**This table is the open decision** — fill the right column when we agree on the roster. A domain can
belong to more than one specialist; `core` is implicit in every persona.

| Domain pack | Namespace(s) | Candidate specialist(s) — **DECIDE** |
|---|---|---|
| `core`     | `DB`, `Creation`, `DB.ExtensibleStorage` | _all (implicit)_ |
| `arch_*`   | `DB.Architecture` (+ DB hosts) | Architectural Senior Modeler |
| `mep_*`    | `DB.Mechanical`, `DB.Plumbing`, `DB.Electrical` | MEP Modeler — *(split into HVAC / Plumbing / Electrical sub-personas?)* |
| `family_*` | `DB` family API | Family Creation Specialist |
| `struct_*` | `DB.Structure`, `…StructuralSections` | Structural Modeler? |
| `coord_*`  | worksharing/links/QA, `DB.IFC` | BIM Coordinator / Manager |
| `viz_*`    | `DB.Visual`, `DB.Lighting` | (Architect? Visualization specialist?) |
| `infra_*`  | `DB.Infrastructure` | (Civil — separate?) |
| `fab_*`    | `DB.Fabrication` | (MEP? dedicated Fabrication?) |
| `analysis_*` | `DB.Analysis` | (MEP / Sustainability?) |

Open sub-questions to settle here: is MEP one specialist or three (HVAC/Plumbing/Electrical)? Is
Structure in scope now? Does the Architect own `viz_*`? Leave as-is until decided.

### Naming convention (decided by this design)
- **Core tools:** flat `verb_noun` — `set_element_parameters`, `get_element_parameters`, `create_view`.
- **Domain tools:** domain-prefixed `domain_verb_noun` — `mep_create_duct`, `arch_create_wall`,
  `family_load`, `struct_create_rebar`, `coord_link_model`. The prefix == the pack == the filter key,
  so persona assembly in §4 is a pure string match on the prefix.

---

## 6. Using the CHM while implementing (workflow)

The 2024 API help is decompiled and indexed for fast lookup:
- Source: `commandset/Utils/2024.chm` (Revit 2024 API, RevitAPI.dll v24.0).
- **In-repo references (committed):**
  `commandset/Utils/RevitAPI_2024_namespaces.md` (namespace inventory table) and
  `commandset/Utils/RevitAPI_2024_classes_by_namespace.txt` (every class grouped by namespace).
- Decompiled topics (regenerate if needed): `D:\tmp\revit2024_chm\html\*.htm` (GUID-named pages).
- Title→file index: `D:\tmp\revit2024_index.tsv` = `<API title>\t<file.htm>` (≈30k entries).
- To regenerate: `hh.exe -decompile . <abs path to 2024.chm>` **run from inside the output dir**
  (cwd matters; it detaches, so poll for output), then grep titles/namespaces from the html.

To get an exact signature before writing a handler:
1. grep the index for the member, e.g. `Floor.Create Method` or `Duct Class`.
2. open the matching `html\<guid>.htm`; the C# `Syntax` block has the precise parameter list, and the
   summary/remarks note transaction/context requirements (e.g. "must be inside a stairs edit scope").

> Do NOT ship the CHM. It lives in `commandset/Utils/` as a dev reference only; it is a source file,
> not build output, so it is not deployed (verified). If you ever add it as `Content`, exclude it
> from the `Commands\` staging glob in `CEM_RibbonUI.csproj`.

---

## 7. Build order (recommendation)

1. **Finish Core** — `get_element_parameters`, `transform_elements`, `create_view`,
   `create_sheet`+`place_viewport`, `tag_elements`, `get_warnings`. Every persona needs these.
2. **MEP pack** (your primary focus) — `create_duct/pipe/conduit/cable_tray`, `connect_mep`,
   `place_mep_equipment`, `create_space`.
3. **Family pack** — project-context first (`load_family`, `place_family_instance`,
   `create_family_type`), then family-editor tools behind `IsFamilyDocument`.
4. **Architecture pack** — `create_wall/floor/ceiling/roof`, `create_opening`.
5. **Coordination pack** — `get_warnings` (early!), `link_model`, `run_interference_check`,
   `manage_worksets`.
6. **Persona delivery** — start prompt-only (#3), graduate to env-var filtering (#2) with the hybrid
   naming from §5.

Each tool still follows the exact 4-part recipe + conventions in `TOOLSET_ROADMAP.md`
(mm in/out, `AIResult` with rich message, never hard-fail a batch, accept names+enums, idempotent,
multi-version safe). This document only adds the **persona/domain layering** on top.
