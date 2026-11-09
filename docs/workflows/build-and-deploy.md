---
title: Build and deploy
type: workflow
updated: 2026-11-09
sources: [commandset/CEM_IAModeler_CommandSet.csproj, plugin/CEM_IAModeler.csproj, server/package.json, CEM_RevitMCP.sln, .mcp.json]
related: [../decisions/0011-cem-ribbonui-hosts-deployment.md, ../modules/server.md, ../modules/commandset.md, smoke-test.md]
tags: [build, deploy]
---

# Build and deploy

## TS server

```bash
cd server
npm install
npm run build      # rimraf build && tsc  → build/index.js  (what the MCP client runs)
npm run bundle     # optional: dist/index.js single self-contained file
```

## C# command set (compile only)

```powershell
dotnet build commandset\CEM_IAModeler_CommandSet.csproj -c "Debug R24" -p:Platform=x64 -p:DeployRevitAddin=false -p:CemRibbonHostBuild=true
```

`-p:CemRibbonHostBuild=true` is what makes it compile-only: the command set's `DeployCommandSet` target keys only on
that flag, so without it a Debug build still copies the output (since 2026-10-07 the add-in DLLs too) to
`plugin\bin\AddIn …` and `%AppData%\Autodesk\Revit\Addins\<ver>\CEM_IAModeler\Commands\`. The add-in
ProjectReferences drop the flag (`GlobalPropertiesToRemove`) but keep `DeployRevitAddin=false`, so they do not
deploy their own manifests either.

**Use a modern .NET SDK** (C# 10 or later). An old `dotnet` on PATH, such as SDK 5.0.103 (C# 9), fails on
`CEM_RevitAuth`'s file-scoped namespaces ("Se esperaba }" / CS1513). Which SDK is where depends on the machine;
nothing pins it (the repo's `global.json` only sets the test runner):
- on 2026-10-06 one machine had only SDK 5.0.103 on PATH and built with the Cemengal-provisioned
  `%LOCALAPPDATA%\Cemengal\dotnet\dotnet.exe` (SDK 10.0.401);
- on 2026-10-07 a workstation without that SDK built R24 and R25 with SDK 9.0.203 from PATH;
- on 2026-11-09 a machine without the provisioned SDK built the plugin with SDK 10.0.301 from PATH
  (CEMENGAL/CEM_AutocadAPI#4).
So run `dotnet --version` first: if it is 8.0 or later (R25+ target `net8.0-windows`), PATH's `dotnet` works; if not,
use the provisioned one. The plugin builds with
`... build plugin\CEM_IAModeler.csproj -c "Debug R24" -p:Platform=x64 -p:CemRibbonHostBuild=true`
(no deploy).

Older docs say `RevitMCPCommandSet.csproj`, but that project no longer exists. Configurations:
`Debug|Release R20…R26`. The plugin (`plugin\CEM_IAModeler.csproj`) needs the sibling
`CEM_RevitAPI` checkout for `CEM_RevitAuth` ([0014](../decisions/0014-license-gated-listener.md)), and since
2026-10-07 the command set needs it too: it builds `CEM_RevitAPI_Extended`, `CEM_Rules` and `CEM_SwapManager`
for the `cem_*` tools and copies them into its output, so `Commands\` carries them
([0025](../decisions/0025-cemengal-addins-as-tools-via-project-references.md)). Pass `-p:DeployRevitAddin=false`
when building the command set alone, or the add-ins it builds deploy their own standalone manifests. R20 builds
without the `cem_*` tools; R26 does not build while `CEM_RevitAPI_Extended` does not (below).
The solution is `CEM_RevitMCP.sln` (plugin, command set and test projects).

## Deploy (normal path: hosted by CEM_RibbonUI)

1. **Close Revit.**
2. Rebuild `CEM_RibbonUI` in the sibling repo
   (`..\..\CEM_RevitAPI\CEM_RibbonUI\CEM_RibbonUI.csproj`, same configuration). It builds both of
   this repo's projects with `CemRibbonHostBuild=true`, stages the command set plus
   `commandRegistry.json` under `Commands\` next to `CEM_IAModeler.dll`, and deploys.
3. A locked-DLL `MSB3231` means Revit was open. That is the deploy step, not a code error.

Why it works this way: [0011](../decisions/0011-cem-ribbonui-hosts-deployment.md).

## Standalone path (the fork's own targets)

The plugin deploys standalone only when no sibling `CEM_RevitAPI/CEM_RibbonUI` exists
([0024](../decisions/0024-standalone-deploy-only-without-ribbon-host.md)); the command set still keys only on
`CemRibbonHostBuild`. A Debug build of the command set without `CemRibbonHostBuild` copies DLLs and `command.json` to
`plugin/bin/AddIn <ver> <cfg>/...` and `%AppData%\Autodesk\Revit\Addins\<ver>\CEM_IAModeler\...`.
The plugin's `CopyFiles` target ships `CEM_IAModeler.addin`. Use this only when CEM_RibbonUI is not
involved; it registers an add-in with **no UI**, so nothing can start the server.

## Start it

In Revit: Cemengal ribbon → **CEM_RevitAI** button (calls `MCPServiceConnection`, license-gated).
A dialog shows "Open Server": it is listening on 8080 and has loaded the commands. A Claude Code session in
this repo reaches it through the `cem-revit-ai` entry in `.mcp.json`, once `npm install` and `npm run build` have
run in `server/`. Point any other MCP client at the local `server/build/index.js` ([smoke test](smoke-test.md)).
