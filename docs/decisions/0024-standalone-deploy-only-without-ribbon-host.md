---
title: "0024 The plugin's standalone deploy runs only when no CEM_RibbonUI sibling exists"
type: decision
status: current
updated: 2026-10-07
sources: [29b3fba, plugin/CEM_IAModeler.csproj, commandset/CEM_IAModeler_CommandSet.csproj]
related: [0011-cem-ribbonui-hosts-deployment.md, ../workflows/build-and-deploy.md, ../modules/plugin.md]
tags: [build, deploy, cross-repo]
---

# 0024 The plugin's standalone deploy runs only when no CEM_RibbonUI sibling exists

Refines [0011](0011-cem-ribbonui-hosts-deployment.md).

## Context

`CemRibbonHostBuild=true` is passed only when MSBuild reaches the plugin **through** CEM_RibbonUI's
`ProjectReference`. But `CEM_IAModeler.csproj` (and `CEM_IAModeler_CommandSet.csproj`) are also
direct members of `CEM_RevitAPI.sln`. `dotnet build CEM_RevitAPI.sln` therefore built the plugin
with the flag unset, and the standalone `CopyFiles` target dropped a `CEM_IAModeler.addin` into the
Revit Addins folder.

That manifest registers a **second** `IExternalApplication` that opens the same MCP socket. The
plugin resolves `Commands/` relative to its own DLL, so it served an **empty**
`commandRegistry.json` (`PathManager` creates one when none exists). The symptom: the connection
succeeds, then every call answers "Method not found". It came back silently after every solution
build.

## Decision (`29b3fba`, 2026-10-07)

`plugin/CEM_IAModeler.csproj` computes:

- `CemRibbonHostPresent = true` when `..\..\..\CEM_RevitAPI\CEM_RibbonUI\CEM_RibbonUI.csproj`
  exists, which means this is a Cemengal checkout.
- `CemStandaloneDeploy = true` only when `CemRibbonHostBuild != true` **and** the host is not
  present.

The `.addin` copy and the `CopyFiles` deploy target now depend on `CemStandaloneDeploy`. In a
Cemengal checkout, the host owns deployment however the build is entered.

## Consequences

- The fork's standalone deploy still works in a checkout without the sibling `CEM_RevitAPI`.
- **Gap (not fixed):** `commandset/CEM_IAModeler_CommandSet.csproj` still keys `DeployRevitAddin` and
  its `DeployCommandSet` target on `CemRibbonHostBuild` only. A direct `CEM_RevitAPI.sln` Debug build
  still copies the command set and `command.json` into
  `%AppData%\Autodesk\Revit\Addins\<ver>\CEM_IAModeler\Commands\CEM_IAModeler_CommandSet\`. That is
  less harmful (no second add-in manifest), but it scatters files. Mirror the guard there if it
  causes trouble ([doc drift](../concepts/doc-drift.md)).
- "Method not found" with a successful connection → look for a stray `CEM_IAModeler.addin` in the
  Addins folder ([smoke test](../workflows/smoke-test.md) triage).
