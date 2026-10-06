---
title: "0011 CEM_RibbonUI hosts the plugin and owns deployment"
type: decision
status: current
updated: 2026-10-06
sources: [b04b41e, aa88b96, plugin/Core/Application.cs, plugin/CEM_IAModeler.csproj, commandset/CEM_IAModeler_CommandSet.csproj]
related: [../workflows/build-and-deploy.md, ../modules/plugin.md, 0014-license-gated-listener.md]
tags: [deployment, cross-repo, build]
---

# 0011 CEM_RibbonUI hosts the plugin and owns deployment

## Context

Upstream ships a standalone add-in: its own `.addin` manifest, its own ribbon panel, and its own
deploy targets. Cemengal already has a ribbon add-in, `CEM_RibbonUI`, in the sibling repo
`CEM_RevitAPI`.

## Decision

`CEM_RibbonUI` (in `../../CEM_RevitAPI/CEM_RibbonUI/CEM_RibbonUI.csproj`) is the host:

- It `ProjectReference`s `plugin/CEM_IAModeler.csproj`, because the ribbon button calls
  `MCPServiceConnection`. It builds `commandset/CEM_IAModeler_CommandSet.csproj` without linking it,
  because the plugin loads that assembly by reflection at runtime. Both builds get
  `CemRibbonHostBuild=true`.
- Its `StageRevitMcpCommandSet` target copies the command set into `Commands\` next to the plugin,
  and copies `command.json` as `Commands\commandRegistry.json`.
- With `CemRibbonHostBuild=true`, the fork's own deploy targets are skipped
  (`DeployRevitAddin=false`, no `DeployCommandSet`/`CopyFiles`), and `CEM_IAModeler.addin` is not
  shipped.
- `plugin/Core/Application.cs` registers **no UI**: the MCP "Switch" button lives on the Cemengal
  tab, to avoid a duplicate panel.

## Consequences

- Rebuild CEM_RibbonUI **with Revit closed**. A locked-DLL `MSB3231` at that step is the deploy
  step, not a code error.
- If no commands load, CEM_RibbonUI was not rebuilt or redeployed.
- This is a cross-repo coupling: changing project names or paths here also requires edits in
  `CEM_RevitAPI` (report these; don't edit them from this repo).
- The fork's standalone build path still exists (`DeployCommandSet`, `CopyFiles` targets) for
  `CemRibbonHostBuild != true`.
