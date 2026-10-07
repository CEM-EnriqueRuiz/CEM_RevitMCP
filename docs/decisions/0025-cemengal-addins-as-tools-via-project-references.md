---
title: "0025 The Cemengal add-ins are cem_* tools that call the add-ins' own code through ProjectReferences"
type: decision
status: current
updated: 2026-10-07
sources: [commandset/CEM_IAModeler_CommandSet.csproj, commandset/Commands/Cemengal/CemengalCommands.cs, commandset/Services/Cemengal/CemengalEventHandlers.cs, commandset/Models/Cemengal/CemengalModels.cs, command.json, plugin/Core/CommandManager.cs, ../CEM_RevitAPI/CEM_RibbonUI/CEM_RibbonUI.csproj]
related: [0001-extend-via-four-part-pattern.md, 0004-prefix-equals-folder-equals-namespace.md, 0011-cem-ribbonui-hosts-deployment.md, 0014-license-gated-listener.md, ../concepts/cemengal-addins-via-mcp.md, ../modules/toolset.md]
tags: [cemengal, add-ins, cross-repo, project-references, csproj]
---

# 0025 The Cemengal add-ins are cem_* tools that call the add-ins' own code through ProjectReferences

## Context

None of the Cemengal add-ins (sibling `CEM_RevitAPI`: CEM Swap, CEM Rules, Filter Manager…) was an MCP
command. The first answer (2026-10-07, same day) documented them in the `cem-aimodeler` skill: post the
ribbon button, or call the add-in's public engine from `send_code_to_revit`. That had three weaknesses:

- **The skill does not ship.** It lives in this repo, so only Claude Code sessions in the workspace read
  it. A user connecting Revit to Claude through the product sees the MCP's tool list, not the skill.
- **It breaks silently.** A snippet naming `SwapRunner.Run` keeps every build green after someone changes
  that signature, and then fails at runtime, in front of a user.
- **It is fragile in use.** About 40 lines of C# per swap, written correctly each time.

The user asked for real tools, referenced as projects "so each time we update code and compile, the MCP
commands update automatically".

## Decision

- **A `cem_*` domain** (prefix `cem_`, folder and namespace suffix `Cemengal`), like `parent_*` /
  `ParentTools`, a domain that is not a Revit API namespace ([0004](0004-prefix-equals-folder-equals-namespace.md)):
  `cem_open_tool`, `cem_swap_find_types`, `cem_swap_list_presets`, `cem_swap_run`, `cem_swap_remove_trial`,
  `cem_rules_apply`.
- **The command set ProjectReferences `CEM_RevitAPI_Extended`, `CEM_Rules` and `CEM_SwapManager`** from
  the sibling checkout (`..\..\..\CEM_RevitAPI\…`). Building CEM_RibbonUI rebuilds the add-ins, the command
  set against them, and stages both; a changed public API breaks this build instead of a user's call.
  - `GlobalPropertiesToRemove="CemRibbonHostBuild"`: built through CEM_RibbonUI, the command set gets
    that flag, and passing it on would make MSBuild build each add-in a second time beside the ribbon's own.
  - Revit 2020 builds without the `cem_*` folder: CEM_RevitAPI has no R20 configuration.
  - `cem_open_tool` also takes the `Nice3point.Revit.Api.AdWindows` reference package.
- **The tools reuse the add-ins' pieces, not copies of them.** `cem_swap_run` resolves a preset as the
  window does: `TypeCatalog`, `SwapSources.OfTypes` + `Allows`, the window's own `ParameterMapViewModel`
  (`Load` + `ToLinks`) over `ParameterCatalog.Of` / `ProbeTarget`, `SwapRunner.Run` with the standing trial
  as `discard`, `TypeValueLedger.ForTrial`, `TransactionNames`; presets go through `SwapPresetStore`
  (inline swaps too, by its own parser). `cem_rules_apply` is `RuleExecutionService.ApplyToModel` on a
  full `XDocument`, exactly the CEM Rules button's call.
- **Add-in types appear only inside `Run` bodies.** `CommandManager` loads the assembly and calls
  `GetTypes()` for every registry entry. A base type, interface or value-type field naming an add-in type
  would make that throw, and all 126 commands would fail to load wherever the add-ins are missing. Inside
  `Run`, a missing add-in fails that one call, inside `Execute`'s `try`, and the event is still set.
- **Copies, not `Private=false`.** The add-in DLLs land in `Commands\` next to the command set. They are
  byte-identical to the ones CEM_RibbonUI loaded at startup (same build, checked by hash), so the command
  set's dependencies bind to the identities already loaded, as the command set's Nice3point and Newtonsoft
  copies already do.

### Rejected

- **Skill and `send_code_to_revit` only** (the morning's answer): the three weaknesses above.
- **Copying add-in logic into the command set** (resolution rules, paths): it would drift from the add-in,
  the opposite of what the user asked. That is why Material Renamer is not a command: its register path and
  rename rules sit inside its command class, so `cem_open_tool "Renamer"` runs the add-in itself.
- **A `cem_swap_delete_old` tool.** CEM Swap's promise is that the user reviews pair by pair and deletes
  only what they accept; the review stays in the CEM Swap window (`cem_open_tool "CEM Swap"`).
- **`Private=false` references**: nothing in `Commands\` to probe, so binding would depend on Revit's own
  `AssemblyResolve` handler finding the already-loaded add-ins.
- **A separate assembly for the `cem_*` tools**: more registry and staging plumbing; the metadata check
  below shows the isolation already holds inside one assembly.

## Consequences

- CEM_RevitMCP's command set now needs the sibling `CEM_RevitAPI` checkout to build, as the plugin already
  did for `CEM_RevitAuth` ([0014](0014-license-gated-listener.md)).
- **CEM_RevitAPI's public API used here is consumed by this repo**: `SwapRunner`, `SwapPlan`, `SwapTrial`,
  `TypeCatalog`, `SwapSources`, `SwapLinks`, `ElementRemover`, `ParameterCatalog`,
  `ParameterMapViewModel`, `SwapPresetStore`, `TypeValueLedger`, `TransactionNames`, `RuleExecutionService`
  and the X-wrappers. A rename there fails CEM_RibbonUI's build until the tool here follows.
- The MCP client gives up after 120 s; `cem_swap_run` and `cem_rules_apply` wait 115 s and then say the run
  is still going in Revit. A second swap run skips what is already replaced (the swap links).
- Verified 2026-10-07: the command set and CEM_RibbonUI build (Debug R24, 0 errors); `Commands\` carries the
  add-ins and a registry of 126 commands; the 12 new schema tests pass (46/46). A metadata scan of the built
  DLL finds add-in types in no base type, interface or value-type field (only reference-type fields of
  compiler-generated lambda caches, which type loading does not resolve). `GetTypes()` itself could not be
  run outside Revit (RevitAPIUI's native dependencies). The command set also builds for R25 (.NET 8). R26 does
  not: `CEM_RevitAPI_Extended` fails on Revit 2026 (`ElementId.IntegerValue` removed), on `origin/master` too,
  which already blocks CEM_RibbonUI's R26 build ([doc drift](../concepts/doc-drift.md)). **Not yet run in a
  live Revit**, and the .NET 8 load contexts of R25/R26 are unverified.
