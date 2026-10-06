---
title: command.json — the command registry
type: module
updated: 2026-10-06
sources: [command.json, plugin/Core/CommandManager.cs, plugin/Configuration/CommandConfig.cs]
related: [../concepts/four-part-tool-pattern.md, ../decisions/0011-cem-ribbonui-hosts-deployment.md, toolset.md]
tags: [registry]
---

# command.json — the command registry

The hand-maintained list of every C# command, at the repo root. **It is the source of truth for
what exists** (120 entries).

```json
{
  "name": "CEM_IAModeler_CommandSet",
  "description": "...",
  "developer": { "name": "Cemengal", ... },
  "commands": [
    { "commandName": "say_hello", "description": "...", "assemblyPath": "CEM_IAModeler_CommandSet.dll" },
    ...
  ]
}
```

- `commandName` must equal the C# `CommandName` and the TS `server.tool` name.
- `assemblyPath` is relative to the plugin's `Commands` directory. A `{VERSION}` placeholder is
  supported by `CommandManager` but not used. All entries say `CEM_IAModeler_CommandSet.dll` (not
  `RevitMCPCommandSet.dll`, as older docs say).
- Optional per-entry fields from `CommandConfig`: `enabled` (skipped when false) and
  `supportedRevitVersions`.

## How it reaches Revit

- **Hosted (normal):** CEM_RibbonUI's `StageRevitMcpCommandSet` copies it as
  `Commands\commandRegistry.json` next to the plugin.
- **Standalone:** the command set's `DeployCommandSet` target copies it as `command.json` into
  `Commands\CEM_IAModeler_CommandSet\`.

When adding a command, also add a one-line `description`. After a batch, check the counts:

```bash
grep -c '"commandName"' command.json
```
