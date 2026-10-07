# references/structure.md — Beams, braces, columns, foundations, framing systems

Covers `struct_*` and `create_structural_framing_system`. Points in mm. Structural elements care about
**level association, justification, and the analytical model**.

## Members
- `struct_create_beam` — single line-based beam start→end (mm) on a level, by **structural framing type**
  name. The beam hosts to the level; its z is the level + offset.
- `struct_create_brace` — line-based brace start→end on a level. Braces typically span between work points
  on different levels — set the endpoints' z accordingly.
- `struct_create_column` — column at a point on a **base level**, by structural column type. Set base and
  **top level** so it spans correctly; a column with only a base level gets a default height.
- `struct_create_foundation` — isolated footing at a point on a level, by foundation type. For wall/slab
  foundations, those are separate (wall foundation hosts to a wall; slab is a structural floor).
- `create_structural_framing_system` — a beam framing system (joists) over a bounded region with spacing/
  direction. Use for repetitive joist layouts rather than placing beams one by one.

## Justification & geometry
- Beam **justification** (z-justification: top/center/bottom; lateral) decides where the section sits
  relative to the locator line. Default is often center — for a top-of-steel datum you usually want top
  justification. Set it via `set_element_parameters` after creation if the type default is wrong.
- Beam/column **start/end level offsets** position the member; resolve levels by name and set offsets in mm.
- Cross-section rotation: set the cross-section rotation parameter for non-vertical webs.

## Analytical model
- New structural elements may carry an **analytical model**. If you only need physical geometry (e.g.
  coordination/IFC reconstruction), you may want to **exclude/disable** the analytical element to avoid
  noise and warnings — do it deliberately via parameters; don't leave orphan analytical bits.
- In Revit 2024 the analytical model is the **separate Analytical Elements** workflow (not 1:1 with
  physical). Don't assume editing physical updates analytical or vice-versa.

## IFC → native (the `parent_*` pipeline)
When reconstructing native structure from a linked IFC (see coordination.md for the full pipeline):
1. `parent_link_extract_geometry` (read) — pull solids from the link.
2. `parent_solid_to_member_params` (read) — infer line/section/level params from each solid.
3. `struct_create_beam` / `struct_create_column` — create native members from those params (compose!).
4. `parent_validate_deviation` (read) — QC the native vs source deviation.

## Sequencing
1. Levels first.
2. Columns (base→top level), then beams (justification set), then braces (cross-level endpoints).
3. Framing systems for repetitive joists.
4. Foundations under columns/walls.
5. Decide analytical inclusion deliberately.
6. Screenshot in 3D + a section — justification and level errors hide in plan.

## Gotchas
- Column with no top level → default height, wrong span.
- Beam center-justified when you wanted top-of-steel → whole frame sits low; fix justification.
- Brace endpoints both on one level → flat brace; set differing z.
- Leftover analytical model → spurious warnings; exclude when only physical is needed.
- Wrong framing type name → silent default substitution; resolve names explicitly.
