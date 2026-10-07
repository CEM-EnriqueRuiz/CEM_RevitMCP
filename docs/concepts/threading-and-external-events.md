---
title: Threading and external events
type: concept
updated: 2026-10-06
sources: [plugin/Core/SocketService.cs, plugin/Core/ExternalEventManager.cs, plugin/Core/CommandExecutor.cs, server/src/utils/ConnectionManager.ts, server/src/utils/SocketClient.ts]
related: [../decisions/0002-command-raises-external-event.md, four-part-tool-pattern.md, ../modules/plugin.md]
tags: [threading, revit-api, upstream]
---

# Threading and external events

**Origin: upstream.** The rule is [0002](../decisions/0002-command-raises-external-event.md).

## The request path

1. An MCP client calls a tool. The TS handler calls `withRevitConnection(...)`
   (`server/src/utils/ConnectionManager.ts`). This **serializes all requests through a mutex**,
   opens a fresh TCP connection to `127.0.0.1:8080` (`REVIT_HOST`/`REVIT_PORT`, [0021](../decisions/0021-loopback-only-socket.md)) (5 s connect timeout), sends one JSON-RPC
   command, and disconnects. Parallel tool calls run one at a time.
2. `RevitClientConnection.sendCommand` (`SocketClient.ts`) buffers the response until the whole JSON
   parses (so large base64 images work), with a **120 s** timeout.
3. Inside Revit, `SocketService` (a `TcpListener` on `SocketService.BindAddress` = `IPAddress.Loopback`, port 8080, hardwired) receives the
   request on a **background thread**, parses and validates it, and calls `CommandExecutor.ExecuteCommand`
   (lookup, execute, audit log). Before [0021](../decisions/0021-loopback-only-socket.md) it bound `IPAddress.Any` and executed directly, bypassing the log.
4. The Command's `Execute` runs on that background thread. It must only parse input, set the
   handler's request, call `RaiseAndWaitForCompletion(ms)`, and return `H.Result`.
5. Revit runs `IExternalEventHandler.Execute(UIApplication)` on the **UI thread** when it is idle.
   That is where all API calls, the `Transaction`, and the completion signal belong.

## Handler skeleton

```csharp
public class MyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
    public MyRequest Request { get; set; }
    public AIResult<MyResult> Result { get; private set; }

    public void Execute(UIApplication app)
    {
        try
        {
            var doc = app.ActiveUIDocument.Document;
            using (var tx = new Transaction(doc, "my_command"))
            {
                tx.Start();
                // per item: try/catch, record warning, continue
                tx.Commit();
            }
            Result = new AIResult<MyResult> { Success = true, Message = "…", Response = … };
        }
        catch (Exception ex) { Result = new AIResult<MyResult> { Success = false, Message = $"Error in my_command: {ex.Message}" }; }
        finally { _resetEvent.Set(); }
    }
    public bool WaitForCompletion(int timeoutMilliseconds = 10000) { _resetEvent.Reset(); return _resetEvent.WaitOne(timeoutMilliseconds); }
    public string GetName() => "my_command";
}
```

## What goes wrong otherwise

- A Revit API call from the Command's `Execute`: intermittent failures, wrong-context exceptions, or
  a hang.
- A missing `finally { _resetEvent.Set(); }`: the command waits for its full timeout, then throws
  `TimeoutException`. The TS client may give up first, at 120 s.
- A modal Revit dialog open: the external event doesn't run until it closes, so the call times out.
- A C# timeout above 120 s is cut off by the TS client first.
