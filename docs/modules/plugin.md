---
title: plugin/ — the Revit add-in (CEM_IAModeler)
type: module
updated: 2026-10-24
sources: [plugin/CEM_IAModeler.csproj, plugin/Core/SocketService.cs, plugin/Core/CommandManager.cs, plugin/Core/CommandExecutor.cs, plugin/Core/MCPServiceConnection.cs, plugin/Core/Application.cs, plugin/Utils/ActionLogger.cs, plugin/Utils/Logger.cs, plugin/Utils/PathManager.cs, plugin/Utils/McpLocalFiles.cs, plugin/Configuration/ConfigurationManager.cs, plugin/UI/CommandSetSettingsPage.xaml.cs]
related: [../decisions/0026-logs-and-artefacts-in-local-temp.md, ../concepts/threading-and-external-events.md, ../decisions/0011-cem-ribbonui-hosts-deployment.md, ../decisions/0014-license-gated-listener.md, ../concepts/audit-trails.md]
tags: [plugin, csharp]
---

# plugin/ — the Revit add-in (CEM_IAModeler)

**Origin: upstream** `RevitMCPPlugin`, renamed by Cemengal to `CEM_IAModeler` (assembly and
namespace) in `aa88b96`. It is the socket service, the command registry and dispatch, and the
external-event plumbing. It holds no tool logic.

| File | Role | Cemengal change |
|---|---|---|
| `Core/Application.cs` | `IExternalApplication`; registers **no UI**, stops the socket on shutdown | UI removed ([0011](../decisions/0011-cem-ribbonui-hosts-deployment.md)) |
| `Core/MCPServiceConnection.cs` | the toggle command the ribbon button calls: Initialize + Start, or Stop | license-gated via `CEM_RevitAuth.AuthenticatedExternalCommand` ([0014](../decisions/0014-license-gated-listener.md)) |
| `Core/SocketService.cs` | singleton `TcpListener(BindAddress = IPAddress.Loopback, 8080)`, hardwired port; on Initialize, sets up `ExternalEventManager` and runs `CommandManager.LoadCommands()`; parses JSON-RPC and dispatches via `CommandExecutor` | loopback bind + audited dispatch ([0021](../decisions/0021-loopback-only-socket.md)) |
| `Core/CommandManager.cs` | reads the registry config and loads `assemblyPath` (relative to `PathManager.GetCommandsDirectoryPath()`), registers the `IRevitCommand` whose `CommandName` matches | none (upstream) |
| `Core/CommandExecutor.cs` | `ExecuteCommand(JsonRPCRequest)`: lookup, `command.Execute(params, id)`, JSON-RPC response | audit hook `ActionLogger.Log` on every path ([0013](../decisions/0013-hardcoded-failsoft-audit-trails.md)) |
| `Core/ExternalEventManager.cs`, `RevitCommandRegistry.cs`, `Settings.cs` | plumbing | none |
| `Configuration/*` | registry/config model (`CommandConfig`: name, assemblyPath, enabled, supported versions); `ConfigurationManager` reads the shipped registry | a missing registry loads an empty config instead of being created ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)) |
| `UI/*` | upstream settings window / command-set page, opened by the `Core.Settings` command | **read-only** ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)): it never rewrites the registry (its upstream load rewrote it empty in the Cemengal layout). Unreachable in the Cemengal build: no button, no `.addin` entry |
| `Utils/ActionLogger.cs` | JSONL audit log, `%TEMP%\Cemengal\RevitMCP\actions-yyyyMMdd.jsonl` ([audit trails](../concepts/audit-trails.md)) | **new**; moved off ACC in [0026](../decisions/0026-logs-and-artefacts-in-local-temp.md) |
| `Utils/Logger.cs` | the RevitMCPSDK `ILogger`, `%TEMP%\Cemengal\RevitMCP\mcp-yyyyMMdd.log` | used to write `<DLL>\Logs\`, i.e. the ACC menu ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)) |
| `Utils/McpLocalFiles.cs` | `App = "RevitMCP"`, the `CemTempFolder` app name shared with the command set | **new** ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)) |
| `Utils/PathManager.cs` | the add-in folder, `Commands` and registry paths, **read only** | creates nothing, writes nothing; `GetLogsDirectoryPath` deleted ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)) |

## Build facts

- Configurations `Debug|Release R20…R26`; net48 for R20–R24, `net8.0-windows` for R25/R26
  (`EnableDynamicLoading`).
- Depends on the `RevitMCPSDK`, `Nice3point.Revit.Api.*` and `Nice3point.Revit.Toolkit` NuGet
  packages, plus a **`ProjectReference` to `../../../CEM_RevitAPI/CEM_RevitAuth`**, so the sibling
  repo must be checked out.
- A **`ProjectReference` to `../../../CEM_CSharpExtended/CEM_CSharpExtended`** for `Hosting.CemTempFolder`, the
  self-cleaning `%TEMP%\Cemengal\RevitMCP` where the logs go
  ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)). `CEM_CSharpExtended.dll` already shipped beside
  `CEM_IAModeler.dll` through `CEM_RevitAuth`.
- Standalone build (`CemStandaloneDeploy`: `CemRibbonHostBuild != true` **and** no sibling
  `CEM_RevitAPI/CEM_RibbonUI`, [0024](../decisions/0024-standalone-deploy-only-without-ribbon-host.md)): the `CopyFiles` target deploys
  `CEM_IAModeler.addin` and the DLLs to `%AppData%\Autodesk\Revit\Addins\<ver>` on Debug builds.
  Hosted build: CEM_RibbonUI owns deployment.
