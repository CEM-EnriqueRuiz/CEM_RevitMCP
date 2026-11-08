---
title: "CEMENGAL/CEM_CSharpExtended#1: C# libraries mirror the native library; here, the compile migration to CEM_CSharpExtended.IO"
type: source
updated: 2026-11-08
sources: [https://github.com/CEMENGAL/CEM_CSharpExtended/issues/1, "root decision 0041 (the user, 2026-11-07)", "the run contract, decision A16 (MCP repos: compile migration only)", "the user, 2026-11-08: commit and push before the reviews finish, so a new project can start on the foundation", commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs, commandset/Services/Core/CoreParamQueryEventHandlers.cs, commandset/Services/Core/CreateSharedParameterEventHandler.cs, commandset/Services/Family/FamilySessionEventHandlers.cs, commandset/Services/Views/ExportViewImageEventHandler.cs, commandset/Utils/McpLocalFiles.cs, plugin/Utils/ActionLogger.cs, plugin/Utils/Logger.cs, plugin/Utils/McpLocalFiles.cs]
related: [../../decisions/0026-logs-and-artefacts-in-local-temp.md, ../../modules/commandset.md, ../../modules/plugin.md, ../../concepts/audit-trails.md, CEM-EnriqueRuiz-CEM_API-82.md]
tags: [issue, cem_csharpextended, naming, refactor, cross-repo]
---
# CEM_CSharpExtended#1: the compile migration to CEM_CSharpExtended.IO

**Issue:** https://github.com/CEMENGAL/CEM_CSharpExtended/issues/1. It is a cross-repo umbrella: every shared C#
library mirrors the native library it extends (root decision 0041). This page covers this repo only.

## What changed here

CEM_CSharpExtended's core now mirrors the BCL's namespaces. `CemTempFolder` and `CemTempFolderUnsafeException` moved
from `CEM_CSharpExtended.Hosting` to `CEM_CSharpExtended.IO`; their members did not change. This repo's 11 C# files
that use them changed one line each, `using CEM_CSharpExtended.Hosting;` → `using CEM_CSharpExtended.IO;`:

| Project | Files |
|---|---|
| `commandset/` | `Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs`, `ExecuteCodeEventHandler.cs`, `Services/AnnotationComponents/TakeScreenshotEventHandler.cs`, `Services/Core/CoreParamQueryEventHandlers.cs`, `CreateSharedParameterEventHandler.cs`, `Services/Family/FamilySessionEventHandlers.cs`, `Services/Views/ExportViewImageEventHandler.cs`, `Utils/McpLocalFiles.cs` |
| `plugin/` | `Utils/ActionLogger.cs`, `Logger.cs`, `McpLocalFiles.cs` |

Behaviour is unchanged: the same folders under `%TEMP%\Cemengal\RevitMCP`, the same guard tests
([0026](../../decisions/0026-logs-and-artefacts-in-local-temp.md)). The tests and builds ran in pass 1 of the run.

## Decisions and why

- **Compile migration only** (the run's decision A16). The MCP repos take the moved names and nothing else. Their own
  duplicate helpers are recorded for their owner (below) rather than refactored now. Rejected: moving them into
  CEM_CSharpExtended in the same pass, which would widen a cross-repo change that the reviewers have not yet finished.
- **A clean cutover** (root 0015): no `[Obsolete]` forwarder in the old `Hosting` namespace, so the old name cannot
  come back.
- **`CemTempFolder` keeps its `Cem` name:** it has no native counterpart (root 0041 §2).
- **Committed before the reviews finish**, by the user's decision, because a teammate is starting a new project on
  this foundation.

## The C# procedure (docs and skills only)

The user asked for the new way to proceed to be written into the wikis and the skills, so that an agent writing C#
here, without being told, puts new code on the mirror structure and migrates the old names in a file it touches. The
canonical procedure has one copy, in CEM_CSharpExtended: the `cem-csharp-mirror` skill and
[csharp-procedure](../../../../../CEM_CSharpExtended/docs/workflows/csharp-procedure.md). This repo points to it from the
`cem-revitmcp-dev` skill (a hard rule before the others), `CLAUDE.md`, `AGENTS.md`, the index (hard rule 13 and a
"Before you change X" row), [commandset](../../modules/commandset.md) and [plugin](../../modules/plugin.md).
- **Pointers, not copies** (CROSS_REPO: one copy). Rejected: restating the procedure here, which would drift.
- **No code change in this step**, by the user's decision. The later helpers below are now migrated by whoever next
  touches those files, under the procedure.
- The Codex entry `.agents/skills/cem-revitmcp-dev/SKILL.md` is unchanged: it only points to the canonical skill.

## Later helpers (recorded, not done)

Candidates for CEM_CSharpExtended's mirrored names (root 0041 §5), for this repo's owner:
- `McpLocalFiles` is duplicated: `IsReparsePoint` and `RefuseReparsePoint` exist in both `plugin/Utils/` and
  `commandset/Utils/`. They belong in `CEM_CSharpExtended.IO` (an `XFile`/`XPath` reparse check).
- `commandset/Services/Family/FamilySessionEventHandlers.cs:142`, `FileName(title)`, strips invalid file-name
  characters by hand → `XPath.ToSafeFileName`.
- `commandset/Services/Views/ExportViewImageEventHandler.cs:162`, `MakeExportNameSafe`, tests a view name against
  `Path.GetInvalidFileNameChars()` and `..` by hand → the same `XPath` safe-name rule.

## Open items at push time

From the issue: the final independent live check, the security review and the acceptance run are pending. Revit was
not checked live with this change. The other open items in the issue (Navisworks tools referencing CEM_Rules,
`WriteTable` edge cases) are not in this repo.
