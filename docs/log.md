---
title: CEM_RevitMCP wiki log
type: log
updated: 2026-11-01
sources: []
related: [index.md]
tags: [log]
---

# Log

Append-only. Heading format: `## [YYYY-MM-DD] ingest|decision|lint|query | <title>`.

## [2026-10-06] ingest | Bootstrap: migrate CLAUDE.md, cem-revitmcp-dev SKILL.md and CEM_RevitMCP.md into the wiki

Migrated all three schema/doc files into modules, concepts, workflows and decisions 0001–0020, and
checked each claim against the code. Doc drift is recorded in [doc drift](concepts/doc-drift.md).
`CEM_RevitMCP.md` is now legacy ([0019](decisions/0019-single-authoritative-doc.md) superseded by
[0020](decisions/0020-wiki-is-the-knowledge-base.md)).

## [2026-10-06] ingest | Commit history 2026-02..2026-04 (upstream mcp-servers-for-revit)

26 upstream commits → [2026-02](sources/commits/2026-02.md), [2026-03](sources/commits/2026-03.md),
[2026-04](sources/commits/2026-04.md). Decisions [0017](decisions/0017-elementid-extensions-over-preprocessor.md)
and [0018](decisions/0018-send-code-transaction-mode.md) were marked upstream.

## [2026-10-06] ingest | Commit history 2026-06 (b04b41e, aa88b96) and the deleted plan docs

Read `TOOLSET_ROADMAP.md`, `SPECIALIST_ARCHITECTURE.md` and `SMOKE_TEST.md` from `b04b41e` →
[2026-06](sources/commits/2026-06.md), decisions 0003–0015.

## [2026-10-06] ingest | CEM-EnriqueRuiz/CEM_API#50 Implement TDD system methodology

The issue has no body or comments. One commit here (`0556fb0`) →
[source](sources/issues/CEM-EnriqueRuiz-CEM_API-50.md), [0016](decisions/0016-schema-only-ts-unit-tests.md).

## [2026-10-06] ingest | Commit history 2026-08 (b05a76f, 915563e, 63b9518)

→ [2026-08](sources/commits/2026-08.md).

## [2026-10-06] decision | 0019 superseded by 0020: docs/ wiki replaces CEM_RevitMCP.md as the knowledge base

## [2026-10-06] lint | Bootstrap lint pass

`wiki_lint.py`: 0 errors after the bootstrap.

## [2026-10-06] ingest | CEM-EnriqueRuiz/CEM_API#68 LLM wiki bootstrap and per-commit ingest workflow

The bootstrap commit, recorded as its own source:
[CEM_API#68](sources/issues/CEM-EnriqueRuiz-CEM_API-68.md). It is the first commit under the
per-commit ingest rule.

## [2026-10-06] decision | 0021 Loopback-only socket, client pinned to 127.0.0.1, dispatch through CommandExecutor (drafted, uncommitted)

Security hardening: `SocketService` bound `IPAddress.Any` (remote code execution through `send_code_to_revit`) and bypassed
`CommandExecutor`, so the audit log was never written. Fixed in code (not yet committed; awaiting an issue URL). Updated 0013, 0014,
the plugin, server, threading, audit-trails, doc-drift, tests and build-and-deploy pages → [0021](decisions/0021-loopback-only-socket.md).

## [2026-10-06] decision | 0022 Local shared-secret handshake proposed as a follow-up

→ [0022](decisions/0022-local-shared-secret-handshake.md).

## [2026-10-07] ingest | Commits 2026-10 pulled from the remote: fe06b6c (no script persistence) and 29b3fba (REFACTOR - Skills)

Rebased the local bootstrap commit onto them. New: [2026-10](sources/commits/2026-10.md), [0023](decisions/0023-send-code-scripts-not-persisted.md)
(supersedes 0013), and [0024](decisions/0024-standalone-deploy-only-without-ribbon-host.md) (refines 0011). Updated 0008, 0011, 0012, 0020, the audit-trails,
send-code, toolset, plugin, build-and-deploy, smoke-test, add-a-tool, finish-a-change, doc-drift and index pages. The remote agent
definition `.claude/agents/cem-revitmcp-dev.md` now reads the wiki first and follows the wiki drift rule, and skill hard rule 11 (no script persistence) was added.

## [2026-10-07] lint | Re-checked earlier claims against the pulled code

Resolved drift: the `CEM_RevitMCP.md` tool-count header (118 vs 120, tag_* consolidation deliberate) and `.gitignore` of `.claude/`.
Corrected: the audit root is now hardcoded in two files, not three. New gap recorded: the command set lacks the plugin's CEM_RibbonUI-sibling deploy guard.

## [2026-10-07] ingest | HOT FIX Revit socket loopback-only and audited dispatch

Committed the 0021 work (drafted 2026-10-06) as a HOT FIX with no issue link →
[2026-10 commits](sources/commits/2026-10.md#hot-fix-2026-10-07-loopback-only-socket-and-audited-dispatch).
0021 is now committed (current), and 0022 stays proposed.

## [2026-10-07] ingest | The cem-aimodeler skill moves into the repo and learns the Cemengal add-ins

The MCP usage skill was a claude.ai account skill; it is now `.claude/skills/cem-aimodeler/` (one copy, Codex entry, `.gitignore` exception),
without the Recipes claims, and with `references/cemengal-tools.md`: which add-in does what, posting a ribbon button, and the CEM Swap,
CEM Rules and Material Renamer engines through `send_code_to_revit` → [2026-10 commits](sources/commits/2026-10.md),
new [Cemengal add-ins through the MCP](concepts/cemengal-addins-via-mcp.md). Updated 0007, doc-drift (row resolved), send-code, index,
`CLAUDE.md`, `AGENTS.md`, `CEM_RevitMCP.md` and `cem-revitmcp-dev` (rule 7 no longer cites the recipe corpus).

## [2026-10-07] decision | 0025 The Cemengal add-ins are cem_* tools calling their own code through ProjectReferences
Same day, superseding the snippet route the skill had first documented: six `cem_*` tools (126 commands), the command set ProjectReferencing
CEM_RevitAPI_Extended, CEM_Rules and CEM_SwapManager → [0025](decisions/0025-cemengal-addins-as-tools-via-project-references.md),
[Cemengal add-ins through the MCP](concepts/cemengal-addins-via-mcp.md) rewritten, [2026-10 commits](sources/commits/2026-10.md) section rewritten;
counts and the `cem_*` domain updated on index, toolset, command registry, commandset, server, overview, four-part pattern, domains, 0004;
build-and-deploy and doc-drift (Extended does not build for R26) updated.

## [2026-10-07] ingest | Commit pass: HOT FIX - MCP Revit to include Cemengal tools
Reviewed and committed the cem_* work → [2026-10 commits](sources/commits/2026-10.md). Fixed on the way: unused rename DTOs removed,
0004 folder list, 0025 API list, stale 120/118 counts, and the compile-only build command (`-p:CemRibbonHostBuild=true`) on
build-and-deploy and `cem-revitmcp-dev`. R24/R25 build, R26 blocked by CEM_RevitAPI_Extended, 46/46 tests; not run in a live Revit.

## [2026-10-08] ingest | qmd MCP through the machine's shared server (root decision 0020)
`.mcp.json`'s `qmd` entry no longer starts a stdio `qmd mcp` per session; it is
`{ "type": "http", "url": "http://127.0.0.1:8181/mcp" }`, the machine's one shared server (root decision 0020: per-session
servers loaded 2 to 3.5 GB of models each and exhausted RAM). The name stays `qmd`, so `enabledMcpjsonServers` is unchanged.
Recorded in [0020](decisions/0020-wiki-is-the-knowledge-base.md) (how to search: scope to `cem_revitmcp`, `lex` first, the CLI
as fallback), the [CEM_API#68 source](sources/issues/CEM-EnriqueRuiz-CEM_API-68.md) (bootstrap stdio wiring, now superseded)
and the index.

## [2026-10-11] ingest | CEM-EnriqueRuiz/CEM_API#74 the request-layer rule in the cem-revitmcp-dev skill
The 2026-10-11 review found no CEM_API request in this repo. At the user's request, hard rule 12 records that any future call goes through CEM_CSharpExtended's `Api` (root 0023). Source page: [CEM_API#74](sources/issues/CEM-EnriqueRuiz-CEM_API-74.md).

## [2026-10-25] ingest | CEM-EnriqueRuiz/CEM_API#82 Nothing writes under ACC or beside the DLL; logs in %TEMP%\Cemengal\RevitMCP
Root decision 0031 applied here as [0026](decisions/0026-logs-and-artefacts-in-local-temp.md) (current). The action log, the plugin
log, screenshots and the shared-parameter and family temp files go through CEM_CSharpExtended's `CemTempFolder` to
`%TEMP%\Cemengal\RevitMCP`. `PathManager` creates nothing beside the DLL, and the settings page is read-only.
`family_save_session` and `create_shared_parameter` never write a file on ACC. The `create_project_parameter` restore moved into
`finally`. A vitest guard inventories every writer (62/62). Live in Revit 2024 the files landed in the user's temp after
CEM_CSharpExtended's `UserTempPath` fix, and the watcher saw 0 ACC events. Source page:
[CEM_API#82](sources/issues/CEM-EnriqueRuiz-CEM_API-82.md). Updated 0023, 0026, audit trails, doc drift, plugin, commandset, tests,
test, smoke test (Claude Desktop now runs the repo build), index, and both skills.

## [2026-11-01] ingest | CEMENGAL/CEM_Workspace#5 the Revit MCP made ready for the modelling agent; decision 0027

From the 2026-11-01 inventory for CEM_Modeler, approved by the user («1. ok»). The source page is
[CEMENGAL/CEM_Workspace#5](sources/issues/CEMENGAL-CEM_Workspace-5.md), an umbrella issue across repos.

- **`family_open_session` activates the family** (`FamilySessionHelpers.Activate`, `OpenAndActivateDocument`). A
  pathless `EditFamily` doc is saved to `Families\` first. `family_save_session` re-activates the project before it
  closes the family → [family editing](modules/family-editing.md).
- **`send_code_to_revit`:**
  - the exact code goes to `code\` before the run;
  - the result and the log line carry the mode, document, outcome and duration;
  - a run that outlives its 60 s timeout adds a `late` line, and one Revit never started never runs;
  - decision [0027](decisions/0027-send-code-scripts-traced-to-local-temp.md) supersedes
    [0023](decisions/0023-send-code-scripts-not-persisted.md)'s no-persistence clause;
  - [0018](decisions/0018-send-code-transaction-mode.md) and [send code](modules/send-code-to-revit.md) now document
    the rollback and timeout semantics.
- **New tool `export_view_image`** (`Views`, Core flat name): one view to a PNG in `images\`, with the detail level in
  an always-rolled-back transaction group. It is tagged `"access": "read"` in `command.json`, the first access tag. The
  counts are now 127 commands and 125 TS tools → [toolset](modules/toolset.md),
  [command registry](modules/command-registry.md).
- **Tests:** `guards/modeller-pins.test.ts` (14) and `tools/export_view_image.test.ts` (7), red first; the
  no-acc-writes inventory gains two writers. 83 tests pass.
- **Wiki:** the counts, the skills (`cem-revitmcp-dev` rules 7, 11 and its counts; `cem-aimodeler` gains
  `export_view_image`), [audit trails](concepts/audit-trails.md), [doc drift](concepts/doc-drift.md).
- **The review fix batch, the same day** (the QA and security quick reviews; 0 blocking):
  - **QA-2:** `family_open_session(familyName)` resolves the family in the open project, or in `hostDocument`, not
    in the active family.
  - **QA-5:** family copies go to a new folder each, never overwritten (0026 amended).
  - **SEC-2 (CWE-22):** `export_view_image` replaces an unsafe view name inside its rolled-back group, and refuses a
    file outside `images\`.
  - **QA-3:** `export_view_image` no longer loses a fast completion as a false timeout.
  - **0027:** a "never ran" line no longer lost when a newer call replaces the run.
  - **Path reuse:** `familyPath` reuses an open document only when it is the same file.
  - **Skills:** both counts given as 125 MCP tools and 127 commands, pinned against the registry.
  - **Tests:** `tools/family_open_session.test.ts` (4) and 10 more pins, which take the suite to 97 tests. 11 were red
    first; the trace-id pin already passed and now locks that behaviour in.
- **SEC-8 consumer follow-up:** CEM_CSharpExtended's `CemTempFolder` now refuses reparse points; that fix is in its
  working tree, not committed. This repo refuses a junction or link at the paths it derives itself:
  - the `Families\<name>_<stamp>\` copy folder (before and after creating it) and its file;
  - the fixed `RevitMCP_SharedParameters.txt` and its folder.
  `family_open_session` reports a refusal clearly. There are 4 pins, red first, and the suite is 103 tests. It was
  built against the local CEM_CSharpExtended working tree, not deployed, and not driven live.
- **QA-7 (the QA recheck, Medium):** with no `hostDocument`, the host is now the active document if it is a project,
  then the open project, then the active document. Before, with two projects open, the first in
  `Application.Documents` won. 2 pins, red first, take the suite to 99 tests. Not driven live: it needs two projects
  open.
- **Second live check (12:57-13:00):** SEC-2, QA-2 with `hostDocument`, QA-5 and the reuse by path passed.
  - Revit 2024's own file name already sanitises `a/../../x`, so the guard is defence in depth.
  - The project-default host is not driven.
  - Details in the [smoke test](workflows/smoke-test.md).
- **Live check, the same day (Revit 2024, after a Debug R24 deploy):** all three fixes passed. The project
  re-activation before a close is the one part not driven. The details are in the
  [smoke test](workflows/smoke-test.md).
