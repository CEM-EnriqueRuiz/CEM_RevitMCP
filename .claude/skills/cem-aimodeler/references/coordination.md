# references/coordination.md — Links, worksets, phases, purge, audit, IFC→native

Covers `coord_*`, `parent_*`, `create_revision`, and model-health tools (`get_warnings`,
`analyze_model_statistics`, `coord_audit_model`).

## Model health (read first)
- `coord_audit_model` — read-only health audit: warning count + top recurring warnings, group types,
  worksharing state. Run this before and after a big operation to see what you changed.
- `get_warnings` — full warnings grouped by description with affected ids. Triage by frequency, not order.
- `analyze_model_statistics` — element counts/complexity. Use to spot bloat (e.g. exploded imports,
  duplicate groups).

## Links
- `coord_link_model` — manage linked models (Revit/IFC/CAD): load/unload/reload, relative vs absolute path.
  Links carry their **own coordinate system**; alignment is via shared coordinates / acquire-coordinates,
  not by moving the link freehand. For STC01 IFC alignment, respect the shared-coordinate transform (the
  180° Z + translation re-base) rather than nudging the link.
- Linked element ids collide with host ids — always qualify with the `RevitLinkInstance`.

## Worksets (worksharing only)
- `coord_manage_worksets` — list/create/set-active/assign worksets. **Fails clearly if the model isn't
  workshared** — check first. New elements land on the active workset; set it deliberately before bulk
  creation so coordination ownership is right.

## Phases
- `coord_manage_phases` — list/set phase, set element phase created/demolished. **Phase creation/ordering is
  limited** (api-gotchas). Elements show per the view's phase + phase filter — a "missing" element is often
  just phase/filter, not deleted. Check `get_current_view_info` phase settings.

## Purge & cleanup
- `coord_purge_unused` — purge unused types/patterns/materials, etc. Run after deleting/reconstructing to
  shed orphans. Purge is aggressive — screenshot/audit after to confirm nothing in-use was culled (it
  shouldn't be, but verify on big models).

## Revisions
- `create_revision` — project revision (description, issued-to/by, issued flag). Sequence + issued state
  drive revision schedules on sheets; create the revision before clouding/tagging for it.

## IFC → native reconstruction (the `parent_*` pipeline)
The senior workflow for turning a linked IFC into native, parametric Revit elements. All `parent_*` tools
except the reconstruct step are **read-only** — they gather, you decide, then you build with native tools.

1. **`parent_link_extract_geometry`** (read) — extract solids/meshes from the linked model.
2. **`parent_solid_to_member_params`** (read) — interpret each solid into member parameters (axis line,
   section, level, justification) — best for linear members (beams/columns/pipes).
3. **`parent_reconstruct_native`** (write) — reconstruct native elements. Or compose: feed the inferred
   params into `struct_create_beam` / `struct_create_column` / `mep_create_*` for finer control.
4. **`parent_validate_deviation`** (read) — QC native vs source: deviation per element, flag outliers.

Compose over monolith: per-domain creators (`struct_*`, `mep_*`, `arch_*`) on top of
`parent_solid_to_member_params` give better types/levels than one blind reconstruct.

## Coordinate alignment note (STC01)
Shared coordinates align by transform, and Revit's **Specify Coordinates / shared-coordinates dialog applies
the inverse** of what you might expect — the value you enter is the survey point's position in the other
system, so a re-base reads as the inverse transform. When re-basing an IFC link with a 180° Z rotation +
translation, set it through acquire/publish shared coordinates, not by moving the instance, so every
discipline stays aligned. Verify with a spot coordinate (`create_spot_dimension`, kind=coordinate) at a
known benchmark.

## Sequencing
1. `coord_audit_model` baseline.
2. Set active workset / phase deliberately.
3. Link + align via shared coordinates.
4. Extract → interpret → reconstruct (compose with native creators) → validate deviation.
5. Purge orphans.
6. Re-audit + screenshot to confirm.

## Gotchas
- Worksets on a non-workshared model → tool fails; check first.
- "Missing" element → usually phase/phase-filter or workset visibility, not deletion.
- Moving a link by hand instead of shared coordinates → misaligns every other discipline.
- Purge before reconstruction finishes → culls types you're about to use; purge **after**.
- Linked ids collide with host ids → qualify with the link instance.
