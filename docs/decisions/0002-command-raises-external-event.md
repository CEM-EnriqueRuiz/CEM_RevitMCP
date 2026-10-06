---
title: "0002 The Command never calls the Revit API; the EventHandler does"
type: decision
status: current
updated: 2026-10-06
sources: [commandset/Commands/Struct/StructCommands.cs, plugin/Core/SocketService.cs, server/src/utils/SocketClient.ts]
related: [../concepts/threading-and-external-events.md, 0001-extend-via-four-part-pattern.md]
tags: [threading, revit-api, upstream]
---

# 0002 The Command never calls the Revit API; the EventHandler does

**Origin: upstream design**, kept unchanged by Cemengal.

## Context

`plugin/Core/SocketService.cs` accepts JSON-RPC requests on a background thread (a `TcpListener` on
port 8080). The Revit API is only valid on Revit's UI thread, inside an API context.

## Decision

The Command (`ExternalEventCommandBase`, from the `RevitMCPSDK` NuGet package) only parses the
`JObject`, hands the request to its handler, raises the `ExternalEvent` and blocks with
`RaiseAndWaitForCompletion(ms)`.

The EventHandler (`IExternalEventHandler, IWaitableExternalEventHandler`) does all the Revit work on
the UI thread. It owns its `Transaction` and signals completion in a `finally`.

## Consequences

- If you break this rule, you get intermittent, hard-to-diagnose failures rather than a clean
  error.
- Timeouts: Cemengal commands default to 20000 ms. Heavy or geometry operations use 60–300 s. The
  TS client also gives up on any request after 120 s (`server/src/utils/SocketClient.ts`), so a C#
  timeout above 120 s will look like a TS-side timeout.
- Details and the handler skeleton: [threading and external events](../concepts/threading-and-external-events.md).
