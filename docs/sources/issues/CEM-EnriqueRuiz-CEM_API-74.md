---
title: "CEM-EnriqueRuiz/CEM_API#74: generic endpoints and one shared request layer per language"
type: source
updated: 2026-10-11
sources: [https://github.com/CEM-EnriqueRuiz/CEM_API/issues/74]
related: [../../../../../docs/decisions/0023-one-shared-request-layer.md, ../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-74.md]
tags: [api, csharp, cem_csharpextended]
---
# CEM_API#74: generic endpoints and one shared request layer per language

**Issue:** https://github.com/CEM-EnriqueRuiz/CEM_API/issues/74. The feature is on the root page [CEM_API#74](../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-74.md).

## What it means for this repo
Root [0023](../../../../../docs/decisions/0023-one-shared-request-layer.md) now covers both languages. A web page calls
CEM_WEB_Library `util/api`. A C# add-in calls a method of CEM_CSharpExtended's `Api` (`CemApiClient`,
`AuthorizedRequests`, `CemApiEndpoints`, `IfcPairJobsClient`…) and never spells a CEM_API route or builds a URL. A
missing call is added to the library first. Routes are generic ([0021](../../../../../docs/decisions/0021-generic-data-endpoints-app-shapes.md)),
and a route an installed add-in uses changes only after a deprecation window ([0025](../../../../../docs/decisions/0025-desktop-client-deprecation-window.md)).

## Review of 2026-10-11
Compliant. The MCP server and its Revit command set make no CEM_API request; their only `localhost` URLs are the server's own.

The rule is in this wiki's Start here and in the dev skill's hard rules, at the user's request.
