---
title: "0020 docs/ wiki is the knowledge base; schema files are short entry points"
type: decision
status: current
updated: 2026-10-06
sources: [CLAUDE.md, AGENTS.md, .claude/skills/cem-revitmcp-dev/SKILL.md]
related: [0019-single-authoritative-doc.md, ../workflows/finish-a-change.md]
tags: [documentation, wiki, process]
---

# 0020 docs/ wiki is the knowledge base; schema files are short entry points

## Context

The knowledge was scattered: `CEM_RevitMCP.md`, a 189-line skill, `CLAUDE.md`, the TDD reference in
the root, and commit history. A new agent saw only the skill, and the skill had drifted from the
code ([0019](0019-single-authoritative-doc.md)). The root schema
([CROSS_REPO.md §9](../../../../CROSS_REPO.md)) asks every repo to keep an LLM-maintained wiki in
`docs/`.

## Decision (bootstrap, 2026-10-06)

- `docs/` holds the conventions, the decisions and their reasons, the modules, the workflows and
  the history (issue and commit sources).
- `CLAUDE.md`, `AGENTS.md` and `.claude/skills/cem-revitmcp-dev/SKILL.md` are short entry points:
  the hard rules plus a redirect to [index.md](../index.md).
- Every commit carries its wiki update in the same commit. A git `pre-commit` gate and Claude Code
  hooks (`.claude/settings.json`) enforce this.

## Consequences

- Finish every change with [finish a change](../workflows/finish-a-change.md).
- `.gitignore` ignores `.claude/` (only the skill is tracked, force-added). The new
  `.claude/settings.json` therefore needs `git add -f`, or a `.gitignore` exception, when it is
  committed.
