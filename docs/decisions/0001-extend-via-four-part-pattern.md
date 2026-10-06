---
title: "0001 Extend the fork through the four-part tool pattern"
type: decision
status: current
updated: 2026-10-06
sources: [b04b41e, aa88b96, command.json, plugin/Core/CommandManager.cs]
related: [../concepts/four-part-tool-pattern.md, ../concepts/upstream-and-fork.md, 0002-command-raises-external-event.md]
tags: [fork, architecture, tools]
---

# 0001 Extend the fork through the four-part tool pattern

## Context

CEM_RevitMCP is a fork of `mcp-servers-for-revit` (see [upstream and fork](../concepts/upstream-and-fork.md)).
Upstream already had a fixed way to add a tool: a TS `server.tool(...)` file, a C# Command, a C#
EventHandler and Model DTOs, plus a `command.json` entry. The root's cross-repo rules
([CROSS_REPO.md §5](../../../../CROSS_REPO.md)) say this fork is extended "via its own documented
4-part pattern".

## Decision

Every Cemengal tool is added with the same four parts plus one registry entry. The plumbing (plugin,
socket, dispatch) is not redesigned:

| Part | Path |
|---|---|
| TS tool | `server/src/tools/<name>.ts` |
| C# Command | `commandset/Commands/<Domain>/` |
| C# EventHandler | `commandset/Services/<Domain>/` |
| C# Models | `commandset/Models/<Domain>/` (or `Models/Common`) |
| Registry | `command.json` |

**Three strings must be byte-identical:** `server.tool("x")`, `CommandName => "x"` and
`command.json`'s `commandName`. The plugin resolves commands only by that string
(`plugin/Core/CommandManager.cs` compares `command.CommandName == config.CommandName`). A mismatch
fails silently: the tool never resolves, and nothing reports it.

## Consequences

- Of the 120 registered commands, 97 were added by Cemengal (`b04b41e`, `aa88b96`) without changing
  the upstream dispatch model.
- `command.json` is the authoritative list. On the TS side, `register.ts` scans the tools folder,
  so there is no list to edit there.
- Several Cemengal files hold more than one Command or EventHandler class. For example,
  `Commands/Struct/StructCommands.cs` holds four commands, and `StructLineMemberEventHandler` serves
  both beam and brace. "One Command + one EventHandler per tool" means per tool *class*, not per
  file.
- Full how-to: [add a tool](../workflows/add-a-tool.md).
