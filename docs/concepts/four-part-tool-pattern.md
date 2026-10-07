---
title: The four-part tool pattern
type: concept
updated: 2026-10-07
sources: [CEM_RevitMCP.md, commandset/Commands/Struct/StructCommands.cs, server/src/tools/register.ts, command.json]
related: [../decisions/0001-extend-via-four-part-pattern.md, ../workflows/add-a-tool.md, threading-and-external-events.md]
tags: [tools, architecture]
---

# The four-part tool pattern

One MCP tool spans both projects. A single command-name string ties them together.

```
server/src/tools/<name>.ts        server.tool("my_command", desc, zodShape, handler)
                                  handler → withRevitConnection(c => c.sendCommand("my_command", params))
        │  TCP JSON-RPC, 127.0.0.1:8080 (loopback only, 0021)
        ▼
commandset/Commands/<Domain>/MyCommand.cs     : ExternalEventCommandBase — parse JObject, raise event, wait
commandset/Services/<Domain>/MyEventHandler.cs: IExternalEventHandler   — Revit work on the UI thread
commandset/Models/<Domain>/MyModels.cs        : request/result DTOs ([JsonProperty])
        │  registered in
        ▼
command.json  →  { "commandName": "my_command", "description": "…", "assemblyPath": "CEM_IAModeler_CommandSet.dll" }
```

**Three names must match byte-for-byte:** `server.tool("my_command")`,
`CommandName => "my_command"` and `command.json`'s `commandName`
([0001](../decisions/0001-extend-via-four-part-pattern.md)).

## Registration is asymmetric

- **TS side: automatic.** `server/src/tools/register.ts` scans its own directory with `readdirSync`,
  dynamically imports every `.ts`/`.js` file except `index.*` and `register.*`, and calls the first
  export whose name starts with `register`. Drop the file in; there is no list to edit. The bundle
  build uses a generated static list instead ([0015](../decisions/0015-single-file-bundle.md)).
- **C# side: hand-maintained.** `command.json` lists every command. At startup,
  `plugin/Core/CommandManager.LoadCommands` loads the assembly named in `assemblyPath` (relative to
  the plugin's `Commands` directory), reflects over every non-abstract `IRevitCommand`, instantiates
  each one (with a `UIApplication` constructor if it has one), and registers the one whose
  `CommandName` equals the config entry.

```bash
grep -c '"commandName"' command.json           # the real count (126)
grep -o '"commandName"[^,]*' command.json      # the list
```

## Canonical Command (compact Cemengal style)

From `commandset/Commands/Struct/StructCommands.cs` and `Commands/ParentTools/`:

```csharp
public class MyCommand : ExternalEventCommandBase
{
    private MyEventHandler H => (MyEventHandler)Handler;
    public override string CommandName => "my_command";
    public MyCommand(UIApplication u) : base(new MyEventHandler(), u) { }
    public override object Execute(JObject p, string r)
    {
        var d = p?.ToObject<MyRequest>();
        if (d == null) throw new ArgumentException("body required");
        H.Request = d;
        if (RaiseAndWaitForCompletion(20000)) return H.Result;   // ms; 60–300 s for heavy/geometry ops
        throw new TimeoutException("my_command timed out");
    }
}
```

Upstream commands use an older, more verbose style: a `data` array, `SetParameters(...)` and
`WaitForCompletion(...)`. Both styles work; new Cemengal tools use the compact one.

## EventHandler

`IExternalEventHandler, IWaitableExternalEventHandler`. It owns its own `Transaction`, returns
`AIResult<T>` with a rich `Message`, never hard-fails a batch, and signals `_resetEvent.Set()` in
`finally`. See [threading and external events](threading-and-external-events.md) and
[AI-grade conventions](ai-grade-conventions.md).

## TS tool

The zod shape mirrors the C# DTO field for field, and every field is `.describe()`d. Call
`sendCommand("name", params)`. The export is named `register<Something>Tool`. To return an image,
return an MCP content block `{ type: "image", data: <base64>, mimeType: "image/png" }` (see
`server/src/tools/take_screenshot.ts`).
