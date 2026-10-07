---
title: Build and deploy
type: workflow
updated: 2026-10-06
sources: [commandset/CEM_IAModeler_CommandSet.csproj, plugin/CEM_IAModeler.csproj, server/package.json, CEM_RevitMCP.sln]
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
dotnet build commandset\CEM_IAModeler_CommandSet.csproj -c "Debug R24" -p:Platform=x64 -p:DeployRevitAddin=false
```

**Use a modern .NET SDK.** On this machine the default `dotnet` on PATH is SDK 5.0.103 (C# 9), which fails
on `CEM_RevitAuth`'s file-scoped namespaces ("Se esperaba }" / CS1513). Use the Cemengal-provisioned
SDK: `%LOCALAPPDATA%\Cemengal\dotnet\dotnet.exe` (SDK 10.0.401 at 2026-10-06). The plugin builds with
`... build plugin\CEM_IAModeler.csproj -c "Debug R24" -p:Platform=x64 -p:CemRibbonHostBuild=true`
(no deploy).

Older docs say `RevitMCPCommandSet.csproj`, but that project no longer exists. Configurations:
`Debug|Release R20…R26`. The plugin (`plugin\CEM_IAModeler.csproj`) needs the sibling
`CEM_RevitAPI` checkout for `CEM_RevitAuth` ([0014](../decisions/0014-license-gated-listener.md)).
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
A dialog shows "Open Server": it is listening on 8080 and has loaded the commands. Then point the
MCP client at the local `server/build/index.js` ([smoke test](smoke-test.md)).
