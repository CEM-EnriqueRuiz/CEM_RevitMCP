# references/architecture.md — Walls, floors, ceilings, roofs, rooms, stairs, openings, areas

Covers `arch_*` and the architectural use of generic creators (`create_grid`, `create_level`,
`create_reference_plane`). All points in mm.

## Datums first
- `create_level` — levels by name + elevation (mm). Place levels **before** level-hosted geometry.
- `create_grid` / `create_multi_segment_grid` — grids with smart spacing / from a polyline chain. Name them
  per the project standard (CMGL numeric/alpha convention) — tags read the grid name.
- `create_reference_plane` — named planes for hosting and dimensioning.

## Walls
- `arch_create_wall` — straight walls between points on a level, by **wall type name**. Base level + height
  (or top-level constraint). Resolve the type by name; a missing type silently substitutes the default.
- `arch_create_curtain_wall` — curtain wall along a line; then `edit_curtain_grid` to add grid lines, and
  `family_*`/panel work for panels/mullions.
- Joins: `arch_join_geometry` / `arch_unjoin_geometry` for wall-to-wall / wall-to-floor cleanups. Join
  changes cut order — screenshot to confirm the right element wins the corner.

## Floors / ceilings / roofs (sketched)
- `arch_create_floor` — closed boundary loop (mm) on a level, by floor type. Loop must be closed, planar,
  contiguous.
- `arch_create_ceiling` — closed boundary in a **ceiling plan / area context**; ceilings host on a level
  with a height offset.
- `arch_create_roof` — **footprint** roof from a closed boundary. (Extrusion/by-face roofs aren't covered by
  this tool — use families or `send_code_to_revit` for those.) Slope is set per-edge after creation.
- `arch_create_opening` — opening in a host (wall/floor/roof/ceiling). For a shaft across levels, use the
  shaft semantics; for a face hole, the host + boundary.

## Rooms & areas
- `arch_create_room` — place rooms at points on a level, **or omit points** to auto-place in all bounded
  regions. Rooms need a bounded enclosure (walls/room separators) or they report "not enclosed".
- `arch_create_separator` — room/area/space separation lines from a polyline, in a view. Use these to close
  gaps so rooms bound correctly.
- `arch_create_area_plan` + `arch_create_area` — area scheme/level area plan, then place areas at points.
- `export_room_data` — pull all room data out for QA/schedules.

## Stairs & railings
- `arch_create_stairs` — straight-run stair between two levels (by-component). Set run width/tread/riser via
  the stair type; the tool wires base/top level.
- `arch_create_railing` — railings on an existing stair/ramp host. Host must exist first.
- Multi-run / sketched stairs aren't in the tool; compose runs or drop to C#.

## Sequencing that works
1. Levels → grids → reference planes.
2. Walls (set top constraints, not just heights, where levels exist).
3. Floors/ceilings/roofs (closed loops).
4. Rooms/separators, then areas.
5. Stairs between levels, then railings on the stair.
6. Openings/joins cleanup.
7. **Screenshot from several angles** — orbit the 3D view; one view hides inside-out floors and mis-leveled
   walls.

## Gotchas
- Boundary loop not closed/contiguous → floor/roof/ceiling rejected.
- Room "not enclosed" → add `arch_create_separator` lines.
- Wall height vs top-constraint: prefer top-level constraint so walls follow level edits.
- Curtain wall: panels/mullions come from the type + `edit_curtain_grid`, not from `arch_create_wall`.
