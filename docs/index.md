---
title: CEM_RevitMCP wiki index
type: index
updated: 2026-10-06
sources: [command.json, CEM_RevitMCP.md, .claude/skills/cem-revitmcp-dev/SKILL.md]
related: [overview.md, log.md]
tags: [index]
---

# CEM_RevitMCP wiki

## Start here

CEM_RevitMCP is a **fork of `mcp-servers-for-revit`**: a TS MCP server (`server/`) that drives Revit
over TCP :8080 through a C# plugin (`plugin/`) and command set (`commandset/`), with 120 commands.
Read [overview.md](overview.md) first. Upstream vs our parts: [upstream and fork](concepts/upstream-and-fork.md).

**Hard rules**

1. A tool = 4 files + a `command.json` entry → [0001](decisions/0001-extend-via-four-part-pattern.md)
2. `server.tool("x")`, C# `CommandName => "x"` and `command.json` `commandName` match byte-for-byte; a mismatch fails silently → [0001](decisions/0001-extend-via-four-part-pattern.md)
3. The Command never calls the Revit API; the EventHandler does, owns the Transaction and signals in `finally` → [0002](decisions/0002-command-raises-external-event.md)
4. Domains come from API namespaces; prefix == folder == namespace suffix → [0003](decisions/0003-domains-from-api-namespaces.md), [0004](decisions/0004-prefix-equals-folder-equals-namespace.md)
5. mm in/out, `AIResult<T>` with a readable `Message`, never hard-fail a batch, names and enums accepted, idempotent by name → [0005](decisions/0005-ai-grade-tool-conventions.md)
6. No full API parity; the long tail goes to `send_code_to_revit` → [0006](decisions/0006-create-methods-not-full-parity.md)
7. Pick new tools from evidence of `send_code_to_revit` fallbacks, not wishlists; scripts themselves are never persisted → [0008](decisions/0008-parent-tools-from-recipe-corpus.md), [0023](decisions/0023-send-code-scripts-not-persisted.md)
8. Family geometry: small tools, on the family doc, no blend/sweep → [0009](decisions/0009-small-composable-family-tools.md), [0010](decisions/0010-no-newblend-use-sloped-extrusions.md)
9. CEM_RibbonUI (sibling `CEM_RevitAPI`) owns deployment; rebuild it with Revit closed → [0011](decisions/0011-cem-ribbonui-hosts-deployment.md)
10. The code wins over `CEM_RevitMCP.md`, which is legacy and partly stale → [0019](decisions/0019-single-authoritative-doc.md), [doc drift](concepts/doc-drift.md)

**Before you change X, read Y**

| Task / area | Read |
|---|---|
| Add or change a tool | [add a tool](workflows/add-a-tool.md), [four-part pattern](concepts/four-part-tool-pattern.md), [toolset](modules/toolset.md) |
| Write a handler (threads, transactions, timeouts) | [threading](concepts/threading-and-external-events.md), [AI-grade conventions](concepts/ai-grade-conventions.md) |
| Choose a name, domain or folder | [domains and naming](concepts/domains-and-naming.md) |
| Decide what to build next / scope | [0006](decisions/0006-create-methods-not-full-parity.md), [0008](decisions/0008-parent-tools-from-recipe-corpus.md), [audit trails](concepts/audit-trails.md) |
| `family_*` tools | [family editing](modules/family-editing.md) |
| `parent_*` / IFC-link reconstruction | [parent tools](modules/parent-tools.md) |
| `send_code_to_revit` | [send code](modules/send-code-to-revit.md) |
| TS server, registration, bundle | [server](modules/server.md), [0015](decisions/0015-single-file-bundle.md) |
| Plugin, socket, dispatch, logging, security | [plugin](modules/plugin.md), [0021](decisions/0021-loopback-only-socket.md), [0013](decisions/0013-hardcoded-failsoft-audit-trails.md), [0014](decisions/0014-license-gated-listener.md), [0022](decisions/0022-local-shared-secret-handshake.md) |
| `command.json` | [command registry](modules/command-registry.md) |
| Csproj, build, deploy, CEM_RibbonUI | [build and deploy](workflows/build-and-deploy.md), [0011](decisions/0011-cem-ribbonui-hosts-deployment.md), [0024](decisions/0024-standalone-deploy-only-without-ribbon-host.md) |
| Tests | [test](workflows/test.md), [0016](decisions/0016-schema-only-ts-unit-tests.md) |
| Pull from upstream / release | [upstream and fork](concepts/upstream-and-fork.md), [release](workflows/release.md) |

**Workflows**: [build and deploy](workflows/build-and-deploy.md) · [test](workflows/test.md) (`cd server && npx vitest run`) · [smoke test](workflows/smoke-test.md) (live Revit)

**Finish**: commit only after the user sends the issue URL. Ingest the change into `docs/` (source page, affected pages, `log.md`) in the same one-line commit → [finish a change](workflows/finish-a-change.md).

## Overview and log

- [overview.md](overview.md): what the repo is, architecture, product-family position, history.
- [log.md](log.md): append-only record of ingests, decisions, lints and queries.

## Modules

- [server](modules/server.md): the TS MCP server; discovery, socket client, bundle.
- [plugin](modules/plugin.md): CEM_IAModeler add-in; socket, registry, dispatch, audit hook.
- [commandset](modules/commandset.md): CEM_IAModeler_CommandSet; domain layout, Models/Common, Utils.
- [command registry](modules/command-registry.md): `command.json`, the source of truth.
- [toolset](modules/toolset.md): all 120 commands by folder, upstream marked, overlaps, build order.
- [parent tools](modules/parent-tools.md): the `parent_*` IFC-link → native pipeline.
- [family editing](modules/family-editing.md): `family_*` tools and the edit session.
- [send_code_to_revit](modules/send-code-to-revit.md): the Roslyn escape hatch.
- [tests](modules/tests.md): the vitest schema suite and the TUnit live-Revit suite.

## Concepts

- [four-part tool pattern](concepts/four-part-tool-pattern.md): anatomy, registration, templates.
- [threading and external events](concepts/threading-and-external-events.md): request path, handler skeleton, timeouts.
- [AI-grade conventions](concepts/ai-grade-conventions.md): helpers to use, message style, batches.
- [domains and naming](concepts/domains-and-naming.md): namespace → prefix → folder map.
- [upstream and fork](concepts/upstream-and-fork.md): what is upstream, what we changed, divergence.
- [audit trails](concepts/audit-trails.md): Screenshots and the JSONL action log (no script persistence).
- [doc drift and latent issues](concepts/doc-drift.md): stale claims and unfixed code issues found at bootstrap.

## Workflows

- [add a tool](workflows/add-a-tool.md): end-to-end procedure and checklist.
- [build and deploy](workflows/build-and-deploy.md): TS, C#, CEM_RibbonUI hosting, starting the server.
- [test](workflows/test.md): vitest loop, TDD exemptions, TUnit tier.
- [smoke test](workflows/smoke-test.md): live end-to-end check and triage.
- [release](workflows/release.md): upstream release machinery (broken for the fork).
- [finish a change](workflows/finish-a-change.md): commit-time wiki ingest.

## Decisions

- [0001](decisions/0001-extend-via-four-part-pattern.md): extend through the four-part pattern (current)
- [0002](decisions/0002-command-raises-external-event.md): the Command never calls the Revit API (current, upstream)
- [0003](decisions/0003-domains-from-api-namespaces.md): domains from API namespaces (current)
- [0004](decisions/0004-prefix-equals-folder-equals-namespace.md): prefix == folder == namespace (current)
- [0005](decisions/0005-ai-grade-tool-conventions.md): AI-grade tool conventions (current)
- [0006](decisions/0006-create-methods-not-full-parity.md): create methods plus common edits, not parity (current)
- [0007](decisions/0007-personas-deferred.md): personas deferred, all tools exposed (current)
- [0008](decisions/0008-parent-tools-from-recipe-corpus.md): `parent_*` from the recipe corpus (current)
- [0009](decisions/0009-small-composable-family-tools.md): small composable family tools (current)
- [0010](decisions/0010-no-newblend-use-sloped-extrusions.md): no blend/sweep; sloped extrusions (current)
- [0011](decisions/0011-cem-ribbonui-hosts-deployment.md): CEM_RibbonUI hosts and deploys (current)
- [0012](decisions/0012-drop-sqlite-store-pack.md): drop the SQLite store pack and dead TS tools (current)
- [0013](decisions/0013-hardcoded-failsoft-audit-trails.md): fail-soft audit trails, hardcoded root (superseded by 0023)
- [0014](decisions/0014-license-gated-listener.md): license-gated listener (current)
- [0015](decisions/0015-single-file-bundle.md): single-file esbuild bundle (current)
- [0016](decisions/0016-schema-only-ts-unit-tests.md): schema-only TS unit tests (current)
- [0017](decisions/0017-elementid-extensions-over-preprocessor.md): ElementIdExtensions over `#if` (current, upstream)
- [0018](decisions/0018-send-code-transaction-mode.md): `send_code_to_revit` transactionMode (current, upstream)
- [0019](decisions/0019-single-authoritative-doc.md): one authoritative doc (superseded by 0020)
- [0020](decisions/0020-wiki-is-the-knowledge-base.md): `docs/` wiki is the knowledge base (current)
- [0021](decisions/0021-loopback-only-socket.md): loopback-only socket, client pinned to 127.0.0.1, audited dispatch (current, our divergence)
- [0022](decisions/0022-local-shared-secret-handshake.md): local shared-secret handshake (proposed)
- [0023](decisions/0023-send-code-scripts-not-persisted.md): no `send_code_to_revit` script persistence; two audit trails (current)
- [0024](decisions/0024-standalone-deploy-only-without-ribbon-host.md): standalone deploy only without a CEM_RibbonUI sibling (current)

## Sources

Issues:

- [CEM-EnriqueRuiz/CEM_API#50](sources/issues/CEM-EnriqueRuiz-CEM_API-50.md): Implement TDD system methodology (`0556fb0`).
- [CEM-EnriqueRuiz/CEM_API#68](sources/issues/CEM-EnriqueRuiz-CEM_API-68.md): LLM wiki bootstrap and per-commit ingest workflow.

Commits without an issue:

- [2026-02](sources/commits/2026-02.md): upstream: initial fork, ElementIdExtensions, TUnit suite, release pipeline.
- [2026-03](sources/commits/2026-03.md): upstream: `send_code_to_revit` transaction mode.
- [2026-04](sources/commits/2026-04.md): upstream: install/schema/addin fixes; last upstream sync `86cf705`.
- [2026-06](sources/commits/2026-06.md): Cemengal: first 53 tools (`b04b41e`), CEM_IAModeler and 120 tools (`aa88b96`).
- [2026-08](sources/commits/2026-08.md): Cemengal: CLAUDE.md and skill, gitignore, commit convention.
- [2026-10](sources/commits/2026-10.md): Cemengal: no script persistence (`fe06b6c`); skills/agents, `.gitignore`, deploy guard (`29b3fba`).
