---
title: Release (upstream machinery, broken for the fork)
type: workflow
updated: 2026-10-06
sources: [.github/workflows/release.yml, scripts/release.ps1, d9054d2, 84325a8, 1849891, 75ae424, 47ddda2, a61a0d8]
related: [../concepts/upstream-and-fork.md, ../concepts/doc-drift.md, build-and-deploy.md]
tags: [release, upstream]
---

# Release (upstream machinery, broken for the fork)

**Origin: upstream** (2026-02-25 commits `d9054d2`…`47ddda2`, version `1.0.0` in `a61a0d8`).

- `scripts/release.ps1`: after a y/N prompt, **discards all uncommitted changes**, checks out
  `main` and pulls. It then writes the version into `server/package.json` and
  `plugin/Properties/AssemblyInfo.cs`, then commits and tags. **Never run it in a working tree that
  holds anyone's uncommitted work.**
- `.github/workflows/release.yml`: on tag, builds the MCP server (Node 18), msbuilds
  `mcp-servers-for-revit.sln` for R20–R26, zips per Revit version, and publishes the npm package
  through OIDC trusted publishing.

## Status for CEM_RevitMCP

**Do not use it.** The solution was renamed to `CEM_RevitMCP.sln` (`aa88b96`). Node 18 is below the
`>=20` the server requires. The npm identity is upstream's `mcp-server-for-revit`. The plugin needs
the sibling `CEM_RevitAPI` checkout, which CI doesn't have. Cemengal distributes the MCP server
through **CEM_RibbonUI**'s build ([build and deploy](build-and-deploy.md)).

Removing or fixing this workflow has not been decided. File an issue before touching it.
