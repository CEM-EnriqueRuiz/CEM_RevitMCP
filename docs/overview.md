---
title: CEM_RevitMCP overview
type: overview
updated: 2026-10-07
sources: [README.md, CEM_RevitMCP.md, command.json, CEM_RevitMCP.sln]
related: [concepts/upstream-and-fork.md, concepts/four-part-tool-pattern.md, modules/toolset.md]
tags: [overview]
---

# CEM_RevitMCP overview

An **MCP server that lets an AI agent drive Autodesk Revit**. It is a **fork** of
[`mcp-servers-for-revit`](https://github.com/mcp-servers-for-revit/mcp-servers-for-revit), extended
by Cemengal from 23 to **126 commands** so Claude can act as a senior Revit modeler (architecture,
MEP, families, structure, coordination, parameters), with `take_screenshot` as its eyes. It is a
shipped Cemengal product feature ("CEM_RevitAI" on the Cemengal ribbon), not just a dev tool.
Upstream vs Cemengal parts: [upstream and fork](concepts/upstream-and-fork.md).

## Architecture

```
MCP client (Claude) ──stdio──► server/ (TS, build/index.js)  ── one tool per file, auto-registered
                                   │ withRevitConnection: mutex, new TCP socket per call
                                   ▼ JSON-RPC over TCP :8080
                    plugin/ CEM_IAModeler.dll (in Revit) ── SocketService → CommandExecutor (+ audit log)
                                   │ CommandManager loads by CommandName (reflection) from command.json
                                   ▼
                    commandset/ CEM_IAModeler_CommandSet.dll ── Command (bg thread) → ExternalEvent
                                                              → EventHandler (UI thread, Revit API, Transaction)
```

| Part | Page |
|---|---|
| `server/`: TS MCP server, 124 tools, vitest schema suite | [server](modules/server.md) |
| `plugin/`: socket, registry, dispatch, audit log, license-gated toggle | [plugin](modules/plugin.md) |
| `commandset/`: Commands / Services / Models / Utils by domain | [commandset](modules/commandset.md) |
| `command.json`: the registry (source of truth, 126 entries) | [command registry](modules/command-registry.md) |
| the tools by domain | [toolset](modules/toolset.md) |
| `tests/commandset/`: upstream TUnit suite (live Revit) | [tests](modules/tests.md) |

How one tool spans all of this: [four-part tool pattern](concepts/four-part-tool-pattern.md). Why
threads matter: [threading](concepts/threading-and-external-events.md).

## Where it sits in the product family

- **Hosted by `CEM_RevitAPI`'s `CEM_RibbonUI`**, which references the plugin, stages the command set
  and `commandRegistry.json`, and owns deployment
  ([0011](decisions/0011-cem-ribbonui-hosts-deployment.md)).
- **Licensed by `CEM_RevitAPI`'s `CEM_RevitAuth`** (a `ProjectReference`;
  [0014](decisions/0014-license-gated-listener.md)).
- The AI-behaviour guide (how to *use* these tools well) is the `cem-aimodeler` skill, outside this
  repo.
- Path: `Cemengal/CEM_IA/CEM_RevitMCP` (flat sibling, not a submodule). Root map:
  [repos/CEM_RevitMCP](../../../docs/repos/CEM_RevitMCP.md) in the root wiki.
- Revit 2020–2026 (net48 for R20–R24, net8 for R25/R26). Node ≥ 20.

## History in one paragraph

Upstream built the architecture and 23 commands (Feb–Apr 2026; last synced `86cf705`). Cemengal
added 53 commands (`b04b41e`, 2026-06-13), then renamed everything to CEM_IAModeler, aligned the
domain folders and reached 120 commands, with audit trails, the license gate, the `parent_*`
pipeline and family-edit sessions (`aa88b96`, 2026-06-15). In August it added agent docs and the
vitest schema suite ([CEM_API#50](sources/issues/CEM-EnriqueRuiz-CEM_API-50.md)). This wiki was
bootstrapped on 2026-10-06 and replaces `CEM_RevitMCP.md` as the knowledge base
([0020](decisions/0020-wiki-is-the-knowledge-base.md)).
