---
title: "0022 Local shared-secret handshake for the Revit socket"
type: decision
status: current
updated: 2026-11-02
sources: ["the user, 2026-11-01 («Con la SEG5 hacemos lo que recomiendas»)", "the security review of CEMENGAL/CEM_Workspace#5 (SEC-5)", plugin/Security/McpAuthToken.cs, plugin/Core/SocketService.cs, plugin/Utils/ActionLogger.cs, plugin/Utils/McpLocalFiles.cs, server/src/utils/SocketClient.ts, plugin/Utils/LogText.cs, plugin/Utils/Logger.cs, "the SEC-5 fix batch, 2026-11-02 (the user: «respecto a SEC-5 hacemos lo que propones»)", server/tests/plugin/runtime.test.ts, server/tests/utils/auth.test.ts, server/tests/guards/socket-auth-pins.test.ts, server/tests/guards/no-acc-writes.test.ts, "the second SEC-5 batch, 2026-11-02 (the user: «2. sí»)", plugin/Core/CommandManager.cs, server/tests/guards/log-format.test.ts]
related: [0021-loopback-only-socket.md, 0014-license-gated-listener.md, 0027-send-code-scripts-traced-to-local-temp.md, ../concepts/audit-trails.md, ../modules/plugin.md, ../modules/server.md]
tags: [security, socket, divergence]
---

# 0022 Local shared-secret handshake for the Revit socket

**Status: current since 2026-11-01** (SEC-5). It was proposed on 2026-10-06 as a follow-up to
[0021](0021-loopback-only-socket.md). It was decided before the CEM_Modeler pilot, in which a Codex agent drives Revit
through this MCP; the user approved it («Con la SEG5 hacemos lo que recomiendas»). Not committed yet.

## Context

After [0021](0021-loopback-only-socket.md), the socket accepts only loopback connections but had no authentication.
Anything that reaches `127.0.0.1:8080` can run `send_code_to_revit`, that is any C#, as the Revit user.

- **This machine has a second local principal:** the Codex sandbox accounts (`<machine>\CodexSandboxUsers`). The same
  holds for other users on a shared workstation or terminal server. This is the real gap.
- A process running as the same user can already run code as that user, so a secret adds little there.
- Browsers cannot practically exploit it. They can't speak raw TCP, and an HTTP request's headers make the plugin's
  JSON parse fail.

## Decision

1. **One token per listener start.** When `SocketService.Start` has bound the port, and before it accepts a client,
   `McpAuthToken.Issue()` makes 32 bytes from a CSPRNG (64 hex characters). It writes them to
   `%LOCALAPPDATA%\Cemengal\RevitMCP\token`: local and per user, never `%TEMP%` (pruned) and never ACC. A second
   Revit that cannot bind the port does not replace the first one's token. `Stop` forgets the token, so every later
   request is refused, and deletes the file when it still holds it. **Revit's closing calls `Stop` itself:**
   - `SocketService.Initialize` subscribes `UIApplication.ApplicationClosing`, so the listener stops and the token is
     revoked whether or not the host calls the plugin's `OnShutdown`. CEM_RibbonUI, which hosts it, has none.
   - There is one subscription at a time. `Stop` removes it, from the instance it was added to, and the next
     `Initialize` adds it again.
   - `Stop` is idempotent, so the plugin's own `OnShutdown` path, kept as it was, finds the listener already stopped.
   - **The port is held exclusively (SEC-5.2, CWE-605, 2026-11-02).** `Start` sets `ExclusiveAddressUse = true` on
     each new listener before it binds, so no other socket, this user's included, can bind `127.0.0.1:8080` while it
     listens. `Stop` drops the listener, so the ribbon's stop/start sets it again.
2. **Only the user and SYSTEM can read it.**
   - The folder and the file get a protected ACL: inheritance off, `FullControl` for the current user's SID and
     LocalSystem, nothing else (not even Administrators, which the profile grants).
   - A folder owned by another account is refused, since an owner can always change its ACL. The owner must be the
     user, SYSTEM or Administrators (what an elevated process creates).
3. **Written atomically, never through a link.**
   - A new temp file is created in the protected folder. It gets its own protected ACL, by path, before the token is
     written into it. Then **one rename** puts it in place, so a reader sees the old file or the new one, never a
     partial one (`McpAuthToken.ReplaceWithRetry`, since the fix batch of 2026-11-02):
     - a POSIX-semantics rename first (`SetFileInformationByHandle`, `FileRenameInfoEx`, `REPLACE_IF_EXISTS |
       POSIX_SEMANTICS`; Windows 10 1709+ on NTFS). It replaces the file even while a reader that shares delete holds
       it, as the TS client's `readFileSync` does; that reader keeps the old content;
     - where that is not supported, `MoveFileEx(REPLACE_EXISTING | WRITE_THROUGH)`, as before;
     - a reader that does not share delete (some scanners) blocks either one while it holds the file, so
       `ERROR_ACCESS_DENIED` and `ERROR_SHARING_VIOLATION` are retried with a backoff of about 1.3 s in all. After
       that, or on any other error, the write fails and the listener does not start, saying why.
     - Before, a reader holding the file made the one `MoveFileEx` fail with Win32 error 5, and the listener did not
       start (QA, 2026-11-02). A test reproduced it: 199 of 200 writes failed under readers.
   - `%LOCALAPPDATA%\Cemengal`, the `RevitMCP` folder and the file are refused if any is a junction or link, before
     and after the write (`McpLocalFiles.RefuseReparsePoint`, the SEC-8 rule).
   - If the token cannot be written, the listener stops and the ribbon command says why. A server nobody can call
     would be no use.
4. **The wire format is a JSON-RPC param:** `params.auth`, on every request. A first-line handshake was rejected: it
   would change upstream's framing on both ends, and the plugin reads one buffer per request.
5. **The plugin checks it before anything runs.** `SocketService.ProcessJsonRPCRequest`:
   - compares the token in constant time (a XOR over every byte of the expected token; net48 has no
     `CryptographicOperations.FixedTimeEquals`), before `CommandExecutor`;
   - answers a missing or wrong token with RevitMCPSDK's `JsonRPCErrorCodes.Unauthorized` (-33200) and the message
     `Unauthorized`, with no detail, and logs `Unauthorized request refused … : <method>`. That line holds the
     method only, and the method is the caller's text, so it is escaped (below) and cut to 80 characters;
   - removes `auth` from the params, so no command, `mcp-*.log` line or `actions-*.jsonl` line ever holds it.
     `ActionLogger` also drops an `auth` key itself, as a second line of defence.
   - The socket no longer traces the raw request (it used to `Trace.WriteLine` the whole message); only its size.
   - **Caller text cannot forge `mcp-*.log` lines (CWE-117, the fix batch of 2026-11-02).** `Logger` passes every format
     argument through `LogText.EscapeArgs`: CR, LF and tab become `\r`, `\n` and `\t`; the other C0 and C1 controls,
     DEL and U+2028/U+2029 become `\uXXXX`. Numbers, dates and enums keep their formatting, and the escaping is
     idempotent. The format strings are the plugin's own, and no logger call builds its message by interpolation, so
     every caller-supplied value (a refused method, a command name, an exception message) goes through it.
     `actions-*.jsonl` needed nothing: its JSON serialisation already escapes the C0 controls, U+0085 and U+2028/U+2029.
6. **The TS server reads the token and recovers from a restart.**
   - `RevitClientConnection` reads the file (trimmed) and sends it with every request.
   - On `Unauthorized` it reads the file again and retries once. Revit restarted and wrote a new token; a refused
     request never ran, so resending it is safe. A second refusal is an error that names the file, never the token.
   - A missing, unreadable or empty file sends nothing. The error names the file, says to start the MCP service in
     Revit, and says the MCP server must run as the same Windows user.
7. **The MCP server runs as the user.** On this machine every file the MCPs write is owned by the user. Under another
   account, such as a sandbox, the token file is unreadable by design, so every call is refused with that clear error.
   This is accepted.
8. **The 120 s client timeout is unchanged**, per attempt. A timed-out request is not retried.

## Rejected

- **A fixed secret in a config file:** it would never rotate, and anyone who read it once could call for ever.
- **Rotating within a session:** the listener's life is the session, and per-start rotation already covers a restart.
- **Windows named pipes with a pipe ACL:** strong, but it replaces upstream's TCP transport on both ends.
- **The token in `%TEMP%\Cemengal\RevitMCP`:** that folder is pruned, and other tools write there.

## Consequences

- An account other than the user's can still connect to `127.0.0.1:8080`, but every request it sends is refused before
  it reaches a command.
- An old TS server build, or the upstream npm server, is refused: rebuild `server/` together with the plugin.
- **Tests:**
  - `server/tests/utils/auth.test.ts` (9) runs the real client against a fake plugin: the token is sent, a restart is
    re-read and retried once, a wrong token is refused after one retry, a missing file sends nothing, the token is
    never in an error or on the console, and the 120 s timeout is not retried.
  - `server/tests/guards/socket-auth-pins.test.ts` (23) pins the C#: the CSPRNG, the path, the ACL, the owner check,
    the closing subscription and its removal on `Stop`, the exclusive port set before each bind,
    the temp file then the move, the reparse checks, the check before `CommandExecutor`, the constant-time compare, the
    stripped param, and no token variable in any log, trace or exception in `plugin/` or `commandset/`.
  - `no-acc-writes` lists `McpAuthToken.cs` as the one writer outside `CemTempFolder`.
  - `server/tests/plugin/runtime.test.ts` (6, since 2026-11-02) loads the built plugin DLL in Windows PowerShell, with no
    Revit:
    - `LogText` leaves no control character or line separator in caller text;
    - 200 token writes into a scratch folder all succeed while the TS client's `readFileSync` loops in a Node process
      and a scanner-like reader that does not share delete keeps opening the file;
    - every read is a whole token, the file never vanishes once written, and the ACL and folder stay clean.
    It skips when the DLL is not built and fails when it is older than its sources. Each mechanism was
    mutation-checked: without the POSIX rename, or without the retry, the 200 writes fail.
- **The exclusive port (SEC-5.2), checked without Revit (2026-11-02)** with plain `TcpListener`s in Windows PowerShell
  5.1 on this Windows 11 machine:
  - Three stop/start cycles on one port, with up to 15 connections left in TIME_WAIT, all restarted. The ribbon
    toggle is not broken.
  - Windows already refused a second bind of `127.0.0.1` on the port, with or without the option: AccessDenied with
    `SO_REUSEADDR`, AddressAlreadyInUse without. The option makes that explicit instead of an OS default.
  - A wildcard (`0.0.0.0`) bind of the same port still succeeds in both modes, but it receives nothing addressed to
    `127.0.0.1`: all 20 test connections reached the loopback listener.
  - While no listener holds the port (stopped, or before Revit starts), anyone can bind it. That fails safe: `Stop`
    has deleted the token file and `Issue` runs only after our bind, so a client finds no token to send.
  - Pinned in `socket-auth-pins`, red first. `SocketService` needs Revit's API, so the runtime test cannot load it.
- **A malformed log format, found in the SEC-5 review, fixed (2026-11-02).** `CommandManager`'s unsupported-version
  warning read `({1}}`, which `string.Format` rejects with a `FormatException`; it now reads `({1})`. The line runs
  only when a command's `supportedRevitVersions` excludes the running Revit, and the exception was caught as a
  failed command load. `server/tests/guards/log-format.test.ts` (3, red first on that line) checks every `_logger`
  message in `plugin/` against .NET's composite format rules and its argument count.
- **Checked without Revit (2026-11-01):** the built R24 DLL was loaded in PowerShell.
  - `Issue` wrote 64 hex characters, and only the exact token matched.
  - `Get-Acl` showed the owner as the user, inheritance off, and only SYSTEM and the user on the folder and the file.
    The sandbox SID was in no entry.
  - A second `Issue` replaced the token and kept the ACL, and `Revoke` deleted the file.
  - A junction planted at the folder was refused, with nothing written through it.
- **Checked live in Revit 2024 (2026-11-01),** after a CEM_RibbonUI Debug R24 deploy, on a new family from
  `CMGL-GenericModel`; detail in the [smoke test](../workflows/smoke-test.md):
  - a call through the TS client worked;
  - raw requests with no token, a wrong one, an empty one and a number got -33200 and ran nothing;
  - `Get-Acl` on the live file showed only the user and SYSTEM;
  - the token was in no log or trace;
  - after a listener restart and after a full Revit restart, the same client process re-read the file and succeeded
    with one retry.
- **Known limitation: a cancelled close of Revit still stops the MCP service** (recorded, not fixed; the fix batch of
  2026-11-02).
  - `ApplicationClosing` is cancellable (a save prompt answered Cancel, another add-in), and `Stop` runs on its first
    notice. So when the close is cancelled, Revit stays open with the service stopped and the token revoked.
  - It fails safe: every call is refused until the ribbon toggle starts the service again, which writes a new token.
  - This repo keeps no separate known-gaps page; this entry is the record.
- **Found live, closed in code: the token file survived a Revit shutdown.**
  - CEM_RibbonUI's `Application` (CEM_RevitAPI) has no `OnShutdown`. So the plugin's `Application.OnShutdown`, which
    stops the listener and revokes the token, never runs in the hosted deployment.
  - It opened nothing: the process had ended, so no listener held the token, and the file stayed readable only by the
    user and SYSTEM.
  - The architect chose the fix here, not in the host: the `ApplicationClosing` subscription in decision 1. An
    `OnShutdown` in CEM_RibbonUI was the alternative; it would depend on every host remembering it.
  - Pinned in `socket-auth-pins`. **Proved live on 2026-11-02** in both reviewers' acceptance run (QA L4, security
    phase C), after a CEM_RibbonUI Debug R24 deploy. Revit was closed normally with the listener running, and then
    `%LOCALAPPDATA%\Cemengal\RevitMCP\token` was gone and nothing listened on 8080. The ribbon toggle off (B1) also
    removed the file, and on again (B2) wrote a new one with the same ACL.
- **Open design question: the listener does not prove itself to the client (SEC-5.3, Info, CWE-940).** Recorded,
  not decided; it is for the user.
  - The token authenticates the client to the listener only. `SocketClient.ts` sends `params.auth` to whoever listens
    on `127.0.0.1:8080` and trusts the answer.
  - The port can be held by someone else while Revit's listener is down: after a crash, which skips
    `ApplicationClosing` and leaves the file, or when another account takes the port first.
  - A squatter gets only a dead token, so this is no bypass of this decision. Each start rotates the token, and Revit
    cannot start while the port is held.
  - But the squatter can feed the agent forged results. Fixing that is outside this decision's one-way design. One
    option the review named: the listener proves the token with an HMAC of a client nonce.
  - No test: it needs a second account holding the port.
