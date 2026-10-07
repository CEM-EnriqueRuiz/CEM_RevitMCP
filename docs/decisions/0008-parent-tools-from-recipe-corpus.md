---
title: "0008 Build the parent_* pipeline tools from the recipe corpus"
type: decision
status: current
updated: 2026-10-06
sources: [aa88b96, commandset/Commands/ParentTools/ParentToolCommands.cs, commandset/Services/ParentTools, commandset/Models/Common/ParentToolModels.cs]
related: [../modules/parent-tools.md, ../concepts/audit-trails.md, 0006-create-methods-not-full-parity.md]
tags: [parent-tools, ifc, recipes, prioritization]
---

# 0008 Build the parent_* pipeline tools from the recipe corpus

## Context

Every `send_code_to_revit` call saves its C# payload to the `Recipes\` audit folder
([0013](0013-hardcoded-failsoft-audit-trails.md)). Analysis of that corpus showed that about 67% of
it was **link-geometry reconstruction**: read geometry from a linked IFC, measure it, recreate it as
native Revit elements, then validate the deviation.

The existing tools were all *authoring* primitives that take clean numeric input, so the AI fell back
to raw code for the whole pipeline. **The real capability gap was reading, not authoring.**

## Decision

Add a `parent_*` pack of high-level **pipeline** tools (not single-API wrappers):

- `parent_link_extract_geometry`
- `parent_solid_to_member_params`
- `parent_reconstruct_native`
- `parent_validate_deviation`

Code: `commandset/Commands/ParentTools/ParentToolCommands.cs`, `Services/ParentTools/`, DTOs in
`Models/Common/ParentToolModels.cs`.

## Consequences

- **Update (2026-10-07):** new scripts are no longer saved ([0023](0023-send-code-scripts-not-persisted.md)).
  The existing `Recipes\` files are a historical corpus. New evidence is how often `send_code_to_revit`
  appears in the `Log\` JSONL, and what the session was doing.
- **When asked what to build next, look at the recipe corpus first.** Choose tools from evidence of
  what the AI actually hand-writes, not from API coverage.
- The audit logs are the success metric: after a change, `Log\actions_*.jsonl` should show the
  specific tools rather than `send_code_to_revit`.
- Module page: [parent tools](../modules/parent-tools.md).
