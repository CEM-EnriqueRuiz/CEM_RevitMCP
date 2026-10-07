# references/mep.md — Duct, pipe, conduit, cable tray, equipment, spaces

Covers `mep_*`. Points in mm. MEP is unforgiving about **systems and connectors** — get those right or
nothing connects/flows.

## Linear runs
- `mep_create_duct` / `mep_create_pipe` / `mep_create_conduit` / `mep_create_cable_tray` — straight segments
  between points (mm). Each needs a **system type** and a **routing/segment type** resolved by name, plus a
  size (diameter or w×h). A missing system type means the run is created "unassigned" and won't behave as a
  network.
- Fittings (elbows, tees, transitions) are auto-inserted at connected ends when geometry meets within
  tolerance and routing preferences allow. If fittings don't appear, the ends aren't actually coincident or
  the routing preference lacks that fitting — check both.
- Slope (pipe): set via the pipe's slope parameter / system; the tool creates flat unless slope is given.

## Systems & flow
- `mep_get_system_info` — read the system an element belongs to (or that an id defines): system type, name,
  flow direction, connected elements. Use this to debug "why won't these connect".
- Flow direction is driven by connectors (supply/return/etc.) and equipment. Don't fight it manually; fix
  the equipment connector assignment.

## Equipment & terminals
- `mep_place_equipment` — place a loadable MEP family (equipment, fixture, terminal, device) by type name at
  point(s). The family must be loaded; the tool activates the symbol.
- Connect runs **to equipment connectors**, not to arbitrary points — route the duct/pipe end to the
  connector location so the network is electrically/hydraulically continuous.

## Spaces
- `mep_create_space` — MEP spaces (the MEP analog of rooms) for loads/airflow. Spaces need bounded
  enclosures or upper-limit settings, like rooms.

## Sequencing
1. Place equipment first (it owns the connectors that anchor the network).
2. Route mains from connectors, then branches.
3. Let fittings auto-insert; verify with `mep_get_system_info`.
4. Add spaces for analysis.
5. Screenshot in 3D and in plan — MEP errors (disconnected, wrong elevation) are obvious on sight.

## Gotchas
- **No system type** → unassigned run, no flow, fittings may not insert.
- Ends "almost" touching → no fitting, no connection. Snap to the connector/point exactly.
- Routing preference missing a fitting size → elbow/tee fails silently; widen the preference or pre-size.
- Elevation: linear MEP is placed at the z you give — a duct at the wrong level reads fine in plan and wrong
  in section. Always check a section screenshot.
- Sizing: set size at creation; resizing after may strand fittings.
