---
title: "0014 The MCP listener only starts for a licensed CEM user"
type: decision
status: current
updated: 2026-10-06
sources: [aa88b96, plugin/Core/MCPServiceConnection.cs, plugin/CEM_IAModeler.csproj]
related: [0011-cem-ribbonui-hosts-deployment.md, ../modules/plugin.md]
tags: [license, security, cross-repo]
---

# 0014 The MCP listener only starts for a licensed CEM user

## Context

Upstream's `MCPServiceConnection` is a plain `IExternalCommand` that toggles the socket service.
Cemengal ships the MCP server as a product feature of its ribbon.

## Decision

`MCPServiceConnection` inherits `CEM_RevitAuth.AuthenticatedExternalCommand`, from
`../../../CEM_RevitAPI/CEM_RevitAuth/CEM_RevitAuth.csproj` (a `ProjectReference`). The sealed
`Execute()` runs `EnsureAuthorized()` first, and `ExecuteAuthorized()` toggles the listener only
with a valid CEM license. This is checked alongside the ribbon button's `Enabled` flag.

## Consequences

- The plugin cannot build without the sibling `CEM_RevitAPI` checkout. This is a cross-repo
  coupling.
- The plugin also needs `Nice3point.Revit.Toolkit`, because the base type is Nice3point's
  `ExternalCommand`.
- Note: the listener binds `IPAddress.Any:8080` (`SocketService.cs`, upstream), not only loopback.
  The license gate controls *whether* it starts, not who can connect.
