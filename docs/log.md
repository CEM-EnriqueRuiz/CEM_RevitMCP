---
title: CEM_RevitMCP wiki log
type: log
updated: 2026-10-06
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
