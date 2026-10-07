# references/families.md — Families, geometry authoring, materials

Covers `family_*` (sessions, parameters, geometry) and `viz_*` (materials). Read before authoring or
editing family geometry, and before any material work.

## Family workflow (the session model)
1. `family_open_session` — opens an `.rfa` for editing. This is the entry point; geometry tools fail clearly
   in a project doc. Screenshot the family doc to orient yourself.
2. Author geometry / parameters (below).
3. `family_save_session` — Save-in-place, or SaveAs to a path, then optionally reload into the project.
4. To get a family into a project: `family_load` (overwrites), then `family_place_instance`.

While a session is open, **all transactions target the family doc**, not the project (see api-gotchas).

## Parameters & types
- `family_add_parameter` — add a family parameter (instance or type), bound to a group, of a given
  `SpecTypeId` (Length, Angle, YesNo, Material, Text…). Get the spec right; you can't easily retype later.
- `family_add_type` / `family_get_types` — add a named type; list existing types with their values.
- `family_set_formula` — drive a parameter from others (`Width = Length / 2`). Formulas are case-sensitive
  and reference parameter **names** exactly; circular refs are rejected.
- `family_associate_parameter` — make geometry parametric by associating a dimension/instance property to a
  family parameter. This is how a flex actually flexes — author the dimension, then associate it.
- `family_add_reference_plane` — named reference planes are the skeleton; dimension geometry **to planes**,
  associate the dimension to a parameter, and the form flexes. Modeling to raw coordinates ≠ parametric.

## Authoring geometry
- `family_create_extrusion` — solid or void extrusion from a closed profile on a sketch plane. Profile must
  be closed, planar, contiguous. For **holes**, add an inner closed loop (a void, or an inner loop in the
  same sketch); winding doesn't matter but loops must not self-intersect. For **arcs**, include arc curves
  in the profile. For a **sloped** top, extrude then use a void/blend, or set the work plane at an angle.
- `family_create_revolution` — solid/void revolve of a profile about an axis line; profile must lie on one
  side of the axis and be closed.
- **Blends / swept blends** — `NewBlend` only; no dedicated tool. Use `send_code_to_revit` inside the family
  doc via `doc.FamilyCreate.NewBlend(...)` (see api-gotchas "no public API").
- Set **solid vs void** explicitly; a void only cuts if it intersects a solid and "Cut Geometry" applies.
- Screenshot each form before saving — sloped panels, holes, and arc profiles are the easy ones to get
  wrong and the obvious ones to catch on sight.

## Placement
- `family_place_instance` places a loaded type at point(s) in mm. The tool activates the symbol first.
- Point-based generic families: `create_point_based_element`. Line-based: `create_line_based_element`.
- Confirm host/level: an unhosted family lands at the level's elevation; a face-hosted one needs a face.

## Materials (`viz_*`)
- `viz_list_materials` — id, name, shading color `[r,g,b]`, transparency.
- `viz_create_material` — **idempotent**: creates, or updates if the name exists. Safe to re-run.
- `viz_assign_material` — assign a material to elements (or to a family's geometry "by category" vs explicit).
- `viz_set_material_appearance` — edit shading graphics: color `[r,g,b]`, transparency `0–100`.
- For a parametric material slot, add a **Material** family parameter and associate it to the geometry's
  material — then the project can swap material per instance/type without editing the family.
- CMGL convention: drive shading color from the project material library; don't hand-pick rogue RGB.

## Common failures
- Placing before activating the symbol → silent no-op (tools handle it; raw C# doesn't).
- Editing geometry with the project doc as the active transaction target → throws.
- Non-closed / self-intersecting profile → extrusion rejected; check loop contiguity.
- Formula referencing a renamed parameter → break; rename via the parameter, not by editing text.
