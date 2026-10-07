---
title: Cemengal add-ins through the MCP
type: concept
updated: 2026-10-07
sources: [commandset/Services/Cemengal/CemengalEventHandlers.cs, commandset/CEM_IAModeler_CommandSet.csproj, command.json, .claude/skills/cem-aimodeler/references/cemengal-tools.md]
related: [../decisions/0025-cemengal-addins-as-tools-via-project-references.md, ../modules/toolset.md, ../modules/send-code-to-revit.md, ../decisions/0011-cem-ribbonui-hosts-deployment.md]
tags: [cemengal, add-ins, cem-tools, skill]
---

# Cemengal add-ins through the MCP

The Cemengal ribbon tab (CEM_RibbonUI, sibling `CEM_RevitAPI` repo) carries the office's own tools: CEM
Swap, CEM Rules, Filter Manager, Nested Manager, Numerate, Renamer and others. Since 2026-10-07 the MCP
reaches them through six `cem_*` tools that call the add-ins' own code, referenced as projects
([0025](../decisions/0025-cemengal-addins-as-tools-via-project-references.md)):

| Tool | What it does | Add-in code it runs |
|---|---|---|
| `cem_open_tool` | press any Cemengal ribbon button for the user, or list them | the button's own command (`PostCommand`) |
| `cem_swap_find_types` | the types CEM Swap lists: placed count, creatable and why not | `TypeCatalog.Read` |
| `cem_swap_list_presets` | the office presets in `SwapPresets.json` | `SwapPresetStore.Load` |
| `cem_swap_run` | a trial on one element, or the run over every candidate | `TypeCatalog`, `SwapSources`, `ParameterMapViewModel`, `SwapRunner.Run` |
| `cem_swap_remove_trial` | take away the standing trial, restore its type values | `ElementRemover.DeleteReplacements` |
| `cem_rules_apply` | every CEM_Rules rule over the whole model, with its report | `RuleExecutionService.ApplyToModel` |

How an agent uses them (which tool for which job, the trial → look → run loop, what stays the user's) is
the [`cem-aimodeler` skill](../../.claude/skills/cem-aimodeler/references/cemengal-tools.md).

## How it stays in step with the add-ins

- `commandset/CEM_IAModeler_CommandSet.csproj` ProjectReferences `CEM_RevitAPI_Extended`, `CEM_Rules` and
  `CEM_SwapManager`. Building CEM_RibbonUI builds them, the command set against them, and stages the command
  set's whole output, add-in copies included, into `Commands\`. A behaviour change in an add-in reaches the
  MCP on the next build; a public API change breaks the build until the tool follows.
- The staged copies are byte-identical to the add-ins CEM_RibbonUI loads at startup, so they bind to the
  same loaded assemblies (one `SwapManager`, one static state).
- Add-in types appear only inside `Run` bodies, never in a base type, interface or value-type field: the
  plugin calls `GetTypes()` on the command set, and a missing add-in must fail one call, not all 126.

## What the tools deliberately do not do

- **Delete old elements after a swap.** CEM Swap keeps them until the user has reviewed every pair, in its
  window (`cem_open_tool "CEM Swap"`).
- **Copy an add-in's logic.** Material Renamer's register path and rename rules live in its command class,
  so it is reached through `cem_open_tool "Renamer"` (it runs straight away, no window), not a tool of its
  own. Filter Manager's services are `internal`; it is a window.
- **Offer Don Limpio** (`CEM_CleanManager`): held back from production (CEM_RevitAPI#133); its button is
  commented out, so `cem_open_tool` does not see it.

## Constraints

- Most Cemengal windows are modal: while one is open, Revit's UI thread is busy and MCP calls time out
  until the user closes it. CEM Swap is modeless.
- The client gives up after 120 s; `cem_swap_run` and `cem_rules_apply` wait 115 s and report that the
  run is still going in Revit.
- Some CEM_Rules rules raise their own error dialogs in Revit (`CalculateWeightRule`,
  `SearchExternalExcelRule`); the user closes them.
- `send_code_to_revit` can still call any public add-in class (it compiles against every loaded
  assembly), but a repeated need is a tool to add here, not a snippet to keep.
