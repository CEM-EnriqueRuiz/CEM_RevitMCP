---
title: "CEM-EnriqueRuiz/CEM_API#68 LLM wiki bootstrap and per-commit ingest workflow"
type: source
updated: 2026-10-06
sources: ["https://github.com/CEM-EnriqueRuiz/CEM_API/issues/68", CLAUDE.md, AGENTS.md, .claude/skills/cem-revitmcp-dev/SKILL.md, .claude/settings.json, .mcp.json]
related: [../../decisions/0020-wiki-is-the-knowledge-base.md, ../../decisions/0019-single-authoritative-doc.md, ../../workflows/finish-a-change.md, ../../concepts/doc-drift.md]
tags: [issue, wiki, process, cross-repo]
---

# CEM-EnriqueRuiz/CEM_API#68 LLM wiki bootstrap and per-commit ingest workflow

- **Tracker:** CEM_API. This is a **cross-repo umbrella issue** ("Mejorar documentación de
  repositorios con LLM-WIKI"). Every repo's bootstrap commit carries this URL.
- **Body / comments:** none. The decisions below come from the bootstrap brief the user reviewed
  and approved.
- Root wiki: decision [0010 LLM wiki per repo](../../../../../docs/decisions/0010-llm-wiki-per-repo.md)
  and feature page [CEM-EnriqueRuiz-CEM_API-68](../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-68.md).

## What changed in this repo

- Built `docs/` from the code, all 32 commits (26 upstream, 6 Cemengal) and the one issue linked in
  history ([CEM_API#50](CEM-EnriqueRuiz-CEM_API-50.md)), plus the three plan docs deleted in
  `aa88b96`. Result: 20 decisions, 9 modules, 7 concepts, 6 workflows, and source pages. Upstream
  parts are marked as upstream ([upstream and fork](../../concepts/upstream-and-fork.md)).
- Slimmed the entry files to redirects: `CLAUDE.md` (40 → 15 lines) and
  `.claude/skills/cem-revitmcp-dev/SKILL.md` (189 → 58 lines, hard rules plus a link to each
  decision).
- Added `AGENTS.md` (Codex reads it from the git root), the Codex discovery entry
  `.agents/skills/cem-revitmcp-dev/SKILL.md` (it was missing, which was drift against the root
  CLAUDE.md), `.claude/settings.json` (SessionStart and PreToolUse wiki hooks) and `.mcp.json`
  (qmd server).

## Decisions and reasons

- **Repo knowledge lives in `docs/`; entry files only redirect there.** A new agent of any provider
  saw only the long skill, which had drifted from the code. → [0020](../../decisions/0020-wiki-is-the-knowledge-base.md),
  superseding the single-doc approach [0019](../../decisions/0019-single-authoritative-doc.md).
- **Every commit carries its wiki update.** Knowledge stays current only if it is recorded by the
  agent that made the change, at the moment it makes it. The git `pre-commit` gate rejects staged
  code without staged `docs/log.md` plus another `docs/` page, and runs the lint.
- **Post-commit backstop.** A commit that slips through (`--no-verify`, a manual commit) is ingested
  by a headless Claude and committed as `DOCS - Wiki ingest <sha>`, so nothing is silently lost.
- **The issue history is harvested once, at bootstrap.** Later ingests come from the implementing
  agent's own knowledge of the change, not from `gh`, because the decisions are made in the session
  that writes the code.
- **qmd is the search tool** (MCP server in `.mcp.json`, `qmd query -c cem_revitmcp` CLI for other
  providers). It is local, hybrid BM25/vector, and has both an MCP and a CLI interface.
- **Rejected by the user:** Obsidian, Marp, Web Clipper, scheduled jobs, and periodic issue
  re-sync. Reason not recorded beyond the user's choice.

## Repo-specific notes

- **Migration:** every section of the old skill, `CLAUDE.md` and `CEM_RevitMCP.md` was moved into
  wiki pages and checked against the code (the table is in the bootstrap report).
  `CEM_RevitMCP.md` stays in the repo as a legacy reference.
- **Drift findings** are recorded in [doc drift and latent issues](../../concepts/doc-drift.md).
  Examples: stale assembly names, the "deliberately kept" SQLite claim, `tag_walls`/`tag_rooms`
  unreachable via MCP, the broken upstream release workflow, and `register.generated`
  double-registration.
- `.gitignore` ignores `.claude/`, so `.claude/settings.json` is committed with `git add -f`.
- The workflow is in [finish a change](../../workflows/finish-a-change.md): ingest into `docs/`, then
  commit code and docs together with one line.

## Commits

| SHA | Date | Subject |
|---|---|---|
| (this commit) | 2026-10-06 | FEAT - LLM wiki docs bootstrap and per-commit ingest workflow. https://github.com/CEM-EnriqueRuiz/CEM_API/issues/68 |
