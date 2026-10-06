---
title: commandset/ — the Revit command set (CEM_IAModeler_CommandSet)
type: module
updated: 2026-10-06
sources: [commandset/CEM_IAModeler_CommandSet.csproj, commandset/Commands, commandset/Services, commandset/Models, commandset/Utils, command.json]
related: [../concepts/four-part-tool-pattern.md, ../concepts/domains-and-naming.md, ../concepts/ai-grade-conventions.md, toolset.md]
tags: [commandset, csharp]
---

# commandset/ — the Revit command set (CEM_IAModeler_CommandSet)

**Origin: upstream** `RevitMCPCommandSet` (23 commands). It was renamed and extended by Cemengal to
120 commands. It builds one `CEM_IAModeler_CommandSet.dll` per Revit version, which the plugin loads
by reflection.

## Layout

```
commandset/
  Commands/<Domain>/   ExternalEventCommandBase subclasses (parse, raise, wait)
  Services/<Domain>/   IExternalEventHandler implementations (all Revit work)
  Models/<Domain>/     request/result DTOs; Models/Common = shared + cross-cutting
  Utils/               shared helpers + Revit 2024 API inventories (dev reference)
```

Domains: `Access, AnnotationComponents, Architecture, Core, DataExtraction, Delete,
ExecuteDynamicCode, Family, Mep, ParentTools, Struct, Test, Views`. The namespace is
`CEM_IAModeler_CommandSet.<Commands|Services|Models>.<Domain>`
([0004](../decisions/0004-prefix-equals-folder-equals-namespace.md)). Which command lives where:
[toolset](toolset.md).

## Models/Common

`AIResult<T>`; geometry DTOs `JZPoint` (with `ToXYZ`, mm → ft), `JZLine`, `JZFace`; and
cross-cutting request/result models: `CoreEditModels`, `GetParametersModels`, `ParameterSetModels`,
`TransformModels`, `TagElementsModels`, `ViewFilterCreationInfo`, `CoordModels`, `VizModels`,
`StyleModels`, `DbGapModels`/`DbGapModels2`, `StructModels`, `ParentToolModels`,
`ScreenshotResult`, and others.

## Utils

| File | Purpose |
|---|---|
| `McpResolveUtils.cs` | units, element/id, category, level, type, curve loop, view resolution |
| `McpParameterUtils.cs` | parameter read/write by name/enum, storage types |
| `McpFamilyUtils.cs` | `OverwriteFamilyLoadOptions`, structural-type parsing |
| `McpMepUtils.cs` | MEP system/type helpers |
| `ElementIdExtensions.cs` | `GetValue()`/`GetIntValue()` across versions ([0017](../decisions/0017-elementid-extensions-over-preprocessor.md)) |
| `GeometryUtils`, `ProjectUtils`, `TransactionUtils`, `DeleteWarningSuperUtils`, `HandleDuplicateTypeUtils`, `JsonSchemaGenerator` | upstream helpers |
| `2024.chm`, `RevitAPI_2024_namespaces.md`, `RevitAPI_2024_classes_by_namespace.txt` | API reference for domain mapping ([0003](../decisions/0003-domains-from-api-namespaces.md)); never shipped |

## Build facts

- Configurations `Debug|Release R20…R26`. Defines: `REVIT2022_OR_GREATER` (R22+),
  `REVIT2023_OR_GREATER` (R23+), `REVIT2024_OR_GREATER` (R24+). R25/R26 target
  `net8.0-windows10.0.19041.0`.
- Packages: `RevitMCPSDK`, `Nice3point.Revit.*`, `Newtonsoft.Json`, `Microsoft.CodeAnalysis.CSharp`
  (Roslyn, for `send_code_to_revit`), and `System.Drawing.Common` on net8 (for `take_screenshot`).
- Standalone build: the `DeployCommandSet` target copies DLLs and `command.json` into
  `plugin/bin/AddIn <ver> <cfg>/CEM_IAModeler/Commands/CEM_IAModeler_CommandSet/` and, on Debug,
  into `%AppData%\Autodesk\Revit\Addins\<ver>\...`. Hosted build: skipped
  ([0011](../decisions/0011-cem-ribbonui-hosts-deployment.md)).

## Code style notes

- Cemengal commands often group several Command classes per file (`StructCommands.cs`,
  `ArchExtraCommands.cs`, `CoreEditCommands.cs`, `DbGapCommands.cs`, `ViewDocCommands.cs`…), with
  their handlers similarly grouped in `Services/<Domain>/*EventHandlers.cs`.
- `ExecuteDynamicCode` keeps its handler next to its command, in `Commands/`.
