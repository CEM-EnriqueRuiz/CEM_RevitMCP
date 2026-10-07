# references/api-gotchas.md — Cross-cutting Revit API rules

Read this before parameter writes, transforms, family loads, or any `send_code_to_revit`. These are the
rules that silently break models when ignored.

## Units
- **Every MCP tool takes mm** for lengths and `[x,y,z]` points; angles in degrees unless stated. The server
  converts mm → internal feet for you. **Never** pre-convert to feet when calling a tool.
- Inside `send_code_to_revit`, the API is in **internal units (decimal feet)**. Convert explicitly:
  `UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters)`. Mixing raw mm into the API is the most
  common geometry-scale bug.

## Transactions
- Every model change inside `send_code_to_revit` must be wrapped in a `Transaction` on the **correct
  document**: the project doc for project elements, the **family doc** while a family session is open.
  Writing to the project while editing a family (or vice-versa) throws or no-ops.
- One transaction per logical change; `Start()` → mutate → `Commit()`. Use `SubTransaction` for nested work,
  `TransactionGroup` + `Assimilate()` to collapse a multi-step op into one undo.
- Regeneration: call `doc.Regenerate()` before reading geometry you just created in the same transaction.

## ElementId
- `ElementId` is **document-scoped and not stable across sessions/links**. Never persist a raw id and expect
  it valid next session — resolve by name/UniqueId. `UniqueId` is stable; ids are not.
- In Revit 2024 `ElementId` wraps `Int64` (`elementId.Value`), not `IntegerValue`. Don't assume 32-bit.
- Across linked models, ids collide — qualify with the `RevitLinkInstance`.

## Types must be active before placement
- A `FamilySymbol` must be activated before you place an instance: `if(!sym.IsActive){ sym.Activate();
  doc.Regenerate(); }`. The `family_place_instance` / `create_point_based_element` tools do this for you;
  raw C# does not.

## Family loading
- `doc.LoadFamily(path, new FamilyLoadOptions(), out family)` — supply an `IFamilyLoadOptions` to resolve the
  "family already exists" prompt (`OnFamilyFound` → set `overwriteParameterValues`, return true). Without it
  a load can silently fail in headless/automation contexts.
- Prefer the `family_load` tool, which overwrites cleanly and reports the result.

## Levels & hosting
- Level-hosted elements (walls base, columns, framing) need a **real level**, resolved by name. A wrong or
  missing level silently sends geometry to elevation 0.
- Face/work-plane-hosted families need a valid host face or `SketchPlane`; set `ActiveView.SketchPlane` or
  pass an explicit plane for model lines.

## Geometry sketches
- Sketched elements (floors, ceilings, footprint roofs, filled regions) need a **closed, planar, non-self-
  intersecting** loop. The tools auto-close and validate; in raw C# build a `CurveLoop` and check
  `loop.IsOpen()` is false. Order matters — curves must be end-to-end contiguous.

## "No public API" list — must use `send_code_to_revit` or accept it can't be done cleanly
- **Scope boxes** — no creation API (workaround: copy an existing one).
- **`NewBlend` / blends** — only via `FamilyItemFactory` inside a family doc; no project-level tool.
- **Rebar, loads, analytical model elements**, boundary conditions — partial/version-specific API.
- **Conceptual mass / divided surfaces** — massing API is limited and brittle.
- **View template *creation* from scratch** (you can apply/duplicate existing) — no clean create.
- **Legends** — cannot be created from nothing; **duplicate** an existing legend (`create_legend` does this).
- **Phase *creation* ordering**, some worksharing display settings — limited.

## Transform / copy
- `transform_elements` (move/rotate/mirror/copy/array) operates in mm and degrees. Rotation is about a
  supplied axis/point; mirroring needs a plane. For a rigid-body re-base (e.g. 180° Z + translation),
  rotate about Z at the pivot, then translate — order matters, and copy-vs-move is explicit.
- `ElementTransformUtils.CopyElements` returns new ids; capture them, don't re-query blindly.

## Batch philosophy
- Tools **skip-and-report** bad items rather than aborting the whole batch. When you script, mirror that:
  collect failures into the result message, keep going, never throw on the first bad element.

## When `send_code_to_revit` is justified
A no-public-tool API (above), a one-off read query, or stitching something no tool composition covers. Keep
the snippet minimal, transaction on the correct doc, **screenshot after** to verify, and say so when you
write the same snippet twice: a repeated snippet is a candidate for a real tool. For a Cemengal add-in's job,
use its `cem_*` tool (`cemengal-tools.md`), not a snippet.
