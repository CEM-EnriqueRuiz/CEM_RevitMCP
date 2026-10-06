---
title: "0019 One authoritative doc (CEM_RevitMCP.md) replaces four plan docs"
type: decision
status: superseded
superseded_by: 0020-wiki-is-the-knowledge-base.md
updated: 2026-10-06
sources: [b04b41e, aa88b96, b05a76f, CEM_RevitMCP.md]
related: [0020-wiki-is-the-knowledge-base.md, ../concepts/doc-drift.md]
tags: [documentation]
---

# 0019 One authoritative doc (CEM_RevitMCP.md) replaces four plan docs

**Status: superseded** by [0020](0020-wiki-is-the-knowledge-base.md).

## Context

The first batch (`b04b41e`) brought three planning docs: `TOOLSET_ROADMAP.md` (the 4-part contract,
templates, conventions, roadmap checklist), `SPECIALIST_ARCHITECTURE.md` (domains from the API,
personas) and `SMOKE_TEST.md`. The old skill mentions four such docs; only three are in git history.
They drifted from the code as tools were added.

## Decision (2026-06-15, `aa88b96`)

All of them were merged into a single `CEM_RevitMCP.md` at the repo root, called "the single source
of truth" and deleted. `CLAUDE.md` and the `cem-revitmcp-dev` skill (`b05a76f`) pointed at it as
authoritative.

## Why superseded

A single hand-maintained doc still drifted: the assembly names, the TS tool count, the smoke-test
path and the release workflow are all stale (see [doc drift](../concepts/doc-drift.md)). It also had
no place for the reasons behind decisions or for rejected approaches. Its content has been migrated
into this wiki, which is now the knowledge base. `CEM_RevitMCP.md` still exists as a historical
reference; prefer the wiki and the code when they disagree with it.
