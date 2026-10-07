# references/cemengal-tools.md — Cemengal's own add-ins, through the MCP

The **Cemengal** ribbon tab carries the office's own tools (swap elements, CEM Rules, filters, nested
families, numbering…). They know the office's parameters, presets and conventions; a hand-made equivalent
built from generic tools or `send_code_to_revit` does not. **When the job is one of these, use the
`cem_*` tools.** They call the add-ins' own code, so they behave exactly like the add-ins.

| Tool | Use it to |
|---|---|
| `cem_open_tool` | open any Cemengal tool for the user (press its ribbon button), or list the buttons |
| `cem_swap_find_types` | find the types to replace and the type to place (read-only) |
| `cem_swap_list_presets` | list the office's saved swaps, `SwapPresets.json` (read-only) |
| `cem_swap_run` | replace elements: a trial on one element, then the run over all |
| `cem_swap_remove_trial` | drop a trial without placing another |
| `cem_rules_apply` | run every CEM Rules rule over the whole model and get its report |

## The tools on the ribbon

| Ribbon panel → button | Add-in | What it does | Through the MCP |
|---|---|---|---|
| Swap → **CEM Swap** | `CEM_SwapManager` | Replaces the elements of one or more types by another type, in the same position, carrying parameter values. Old elements stay until reviewed. | `cem_swap_*` (§ Replacing elements); the review in its window |
| Rules → **CEM Rules** | `CEM_Rules` | Runs the parameter-derivation rules stored in the model over the whole model. | `cem_rules_apply` |
| Material → **Renamer** | `CEM_MaterialRenamer` | Renames materials from the office register Excel. No window: it runs straight away and reports in a dialog. | `cem_open_tool "Renamer"` |
| Filters → **Filter Manager** | `CEM_FilterManager` | View filters: naming standard, audit (name, use, duplicates), batch rename/delete, create missing ones. | `cem_open_tool` |
| Selection → **Nested Manager** | `CEM_NestedManager` | Nested components of operative families, 3D preview, and the CEM_Rules editor. Opens on the current selection. | `cem_open_tool` |
| Parameters → **CopyValueTo** | `CEM_PassValue` | Copies parameter values between elements / into families. | `cem_open_tool` |
| Parameters → **Convert Inventor** | `CEM_TransferParametersINV` | Maps parameters of Inventor-exported families to the office parameters. | `cem_open_tool` |
| Numerate → **Numerate** | `CEM_Numerate` | Numbers elements by position with the configs in `Numerate.json` for the project's `CEM_Project_Key`. | `cem_open_tool` |
| Pipes → **Place Pipes** | `CEM_PlacePipes` | Splits a picked pipe into stock-length segments with unions (the user picks). | `cem_open_tool` |
| Sheets → **Review Fabrication** | `CEM_TagFabricationSheet` | Fills fabrication-sheet parameters (material, weight, scale, revision) from the family on the sheet. | `cem_open_tool` |
| Sheets → **Sections Visibility** | `CEM_SectionsInSheets` | Hides/restores section markers per sheet. | `cem_open_tool` |
| Fabrication → **Export SAT files** | `CEM_Fabrication3DExporter` | Exports each `CEM_Posicion` group to a SAT file (asks for a folder). | `cem_open_tool` |
| CEM_AI → **CEM_AIModeler** | MCP plugin | Opens/closes this MCP connection. `cem_open_tool` refuses it. | — |

Not available: **Don Limpio** (`CEM_CleanManager`) is held back from production (CEM_RevitAPI#133); its
button is hidden. When the user names a job ("swap these", "number the plates", "check the filters"),
map it to this table first.

## Opening a tool for the user (`cem_open_tool`)

- Call it without `tool` to list the buttons with their add-in and whether they are enabled.
- `tool` takes the label or the add-in name, ignoring case and spaces: `"CEM Swap"`, `"SwapManager"`,
  `"nested manager"`.
- **Select first** when the tool works on the selection (Nested Manager opens on it; CEM Swap's trial uses
  a selected candidate) — e.g. with the selection tools — then open it.
- **Most windows are modal**: while one is open, every MCP call times out until the user closes it. Tell
  the user what to do in the window before opening it, and don't call tools meanwhile. CEM Swap is
  modeless: the MCP keeps working while it is open.
- A disabled button means the Cemengal license is not valid: the user fixes that (CEM_License panel).

## Replacing elements (CEM Swap)

What CEM Swap guarantees, and a hand-made swap (place + copy values + delete) loses: the new element takes
the old one's position, level, host, work plane and flips; an offset in the old element's own axes corrects
families modelled with a different origin; one new element can go on **each hole** of the old one
(markers); the parameter map carries values by name, from instance or type, constants and CEM_Rules
expressions; nested components and group members are left out and counted; every new element keeps a
link to its old one, so a second run never stacks duplicates; and **old elements are never deleted by a
run**. One run = one transaction = one Ctrl+Z.

1. **Know the swap.** `cem_swap_list_presets` — prefer an office preset when one fits. Otherwise find the
   types with `cem_swap_find_types` (`text` filters by name): the types to replace need `placed > 0`, the new
   type must be `creatable`. Names are matched exactly as these tools return them.
2. **Trial.** `cem_swap_run` with `preset` (or an inline `swap`: `sources`, `target`, `links`, `offset`,
   `conditions`, `placement`, `watches`), `mode: "trial"`. It replaces one element — the selected one when
   it is a candidate — and selects it. The report gives the candidates, what the conditions excluded, group
   members and already-replaced elements, the parameter **map** actually used (same-name instance
   parameters are added, as in the window), and per value whether it landed.
3. **Look.** `take_screenshot` (zoom to the selection) and `get_element_parameters` on the new id. Wrong
   position → change the offset (mm, old element's axes) and run the trial again: it replaces the previous
   trial. Missing values → add links. To give up: `cem_swap_remove_trial`.
4. **Run all.** `cem_swap_run` with `mode: "all"`: removes the trial and replaces every candidate in one
   transaction. Report placed, refused (with each refusal) and the pairs with issues; screenshot a sample.
5. **The review is the user's.** Old elements are still in the model. Open CEM Swap
   (`cem_open_tool "CEM Swap"`): it reads the links and lists every pair, with zoom, to accept and delete
   only the accepted old elements. Don't delete old elements yourself.

Inline `swap` details: a link is `{ target, targetScope, source, sourceScope }`, or `{ target, constant }`,
or `{ target, expression }` with `{source parameter}` tokens; scopes are `Instance` / `Type`. A `Type`
target is shared by every element of the type: written once, and disagreeing old values are reported, not
settled. `placement.mode: "OpeningCentres"` puts one element on each hole of the old one.

## CEM Rules (`cem_rules_apply`)

Runs every rule stored in the model over the whole model, as the **CEM Rules** button does (general rules
from Project Information, then specific ones). It writes parameters across the whole model: say so and ask
before running it. It returns the report the button shows in dialogs: errors, failed passes, failed
elements, elements owned by other users (skipped), types missing parameters, unique values suffixed `_N`.
Some rules raise their own error dialog in Revit; the user closes it. Editing rules is Nested Manager's
job (`cem_open_tool`).

## When it takes long

The MCP gives up after 2 minutes. `cem_swap_run` (mode all) and `cem_rules_apply` wait 115 s and then say
the run is still going in Revit. Wait, look (`take_screenshot`, `cem_swap_find_types` counts), and only then
run again: a second swap run skips what is already replaced.
