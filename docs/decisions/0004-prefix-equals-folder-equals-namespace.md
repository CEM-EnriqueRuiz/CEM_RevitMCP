---
title: "0004 Tool prefix == folder == namespace suffix"
type: decision
status: current
updated: 2026-10-06
sources: [b04b41e, aa88b96, commandset/CEM_IAModeler_CommandSet.csproj]
related: [0003-domains-from-api-namespaces.md, 0007-personas-deferred.md, ../concepts/domains-and-naming.md]
tags: [naming, folders]
---

# 0004 Tool prefix == folder == namespace suffix

## Context

The first batch (`b04b41e`) created mixed folders: `Commands/Arch`, `Models/MEP`, and some commands
at the `Commands/` root. Commands, Services and Models drifted apart (`Models/MEP` vs
`Services/Mep`, `Structure` vs `Struct`, `Annotation` vs `AnnotationComponents`).

## Decision

On 2026-06-14 (landed in `aa88b96`), the three trees were aligned. Folder name == C# namespace
suffix (`CEM_IAModeler_CommandSet.Commands.<Domain>`) == the `domain_` tool prefix.

- Core tools: flat `verb_noun` (for example `set_element_parameters`, `create_view`).
- Domain tools: `domain_verb_noun`, one of `arch_*`, `mep_*`, `family_*`, `struct_*`, `coord_*`,
  `viz_*`, `parent_*`.
- Canonical folders: `Core, Access, Architecture, Mep, Family, Struct, Views, AnnotationComponents,
  DataExtraction, Delete, Test, ExecuteDynamicCode, ParentTools`, plus `Models/Common`.

The prefix is also meant to be the future persona-filter key; see [0007](0007-personas-deferred.md).

## Consequences

- Runtime never depends on folders or namespaces (resolution is by `CommandName`), so this rule is
  for navigation and filtering only.
- NTFS case-only renames (`MEP` → `Mep`) need a two-step rename to take effect on disk.
- Exceptions in today's code:
  - `Commands/ExecuteDynamicCode/` holds both its Command and its EventHandler; there is no
    `Services/ExecuteDynamicCode/`.
  - Many DTOs for Core, Coord, Viz, Struct and ParentTools live in `Models/Common` (for example
    `StructModels.cs`, `ParentToolModels.cs`, `CoordModels.cs`).
  - `delete_element` lives in `Commands/Delete/`, but its batch sibling `delete_elements` lives in
    `Commands/Core/CoreEditCommands.cs`. Always locate a command by grepping its `CommandName`
    string, not by guessing the folder.
