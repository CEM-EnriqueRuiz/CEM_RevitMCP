---
title: "0007 Specialist personas are deferred; all tools stay exposed"
type: decision
status: current
updated: 2026-10-07
sources: [b04b41e, aa88b96, server/src/tools/register.ts]
related: [0004-prefix-equals-folder-equals-namespace.md, 0006-create-methods-not-full-parity.md]
tags: [personas, design]
---

# 0007 Specialist personas are deferred; all tools stay exposed

## Context

`SPECIALIST_ARCHITECTURE.md` (`b04b41e`) designed "specialist personas" (Architectural Senior
Modeler, MEP Modeler, Family Creation Specialist, and so on) on top of one shared Core pack. A
persona would be a system prompt plus a tool subset; it would not be a C# construct. Three delivery
options were weighed:

1. Separate MCP server entry points per persona (`server/src/personas/*.ts`).
2. One server, filtered by an environment variable (`REVIT_PERSONA=mep` → `register.ts` loads only
   Core + `mep_*`).
3. One server exposing everything, with the persona living only in the system prompt.

The plan recommended starting with #3 and moving to #2 above roughly 40 tools. The
domain-to-specialist mapping table was left explicitly "TO DECIDE" (open questions: one MEP persona
or three, whether Structure is in scope, whether the Architect owns `viz_*`).

## Decision

Persona delivery is **deferred**. All tools are exposed by one server (option #3 in effect), and no
persona filtering exists in `register.ts`. Revisit env-var prefix filtering once the surface
warrants it. The naming convention ([0004](0004-prefix-equals-folder-equals-namespace.md)) keeps
that a pure prefix match, so no per-tool metadata is needed.

The reason the #2 threshold (~40 tools) was passed without filtering is not recorded.

## Consequences

- The domain-to-specialist mapping is still undecided.
- The AI-behaviour side ("act as a senior Revit modeler") lives in the `cem-aimodeler` skill,
  `.claude/skills/cem-aimodeler/` (outside this repo, as a claude.ai account skill, until 2026-10-07;
  see [Cemengal add-ins through the MCP](../concepts/cemengal-addins-via-mcp.md)).
