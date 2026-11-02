// Pins decision 0022 (docs/decisions/0022-local-shared-secret-handshake.md, SEC-5) in the plugin's C# source, which runs
// only inside Revit (exempt, 0016), as no-acc-writes.test.ts does for root decision 0031. The Revit socket runs any C#
// through send_code_to_revit, and this machine has other local accounts (the Codex sandbox) that can reach 127.0.0.1:
// 1. each listener start writes a fresh 32-byte CSPRNG token to %LOCALAPPDATA%\Cemengal\RevitMCP\token, readable only
//    by the user and SYSTEM, atomically, never through a junction or link;
// 2. a request without the token is refused before CommandExecutor, in constant time, with no detail;
// 3. the token never reaches a log, a trace or the commands. The client side is tests/utils/auth.test.ts.
import { describe, expect, it } from "vitest";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "..");
const read = (path: string) => readFileSync(join(repo, path), "utf8");
// Comments may describe what is pinned; only code counts.
const code = (text: string) => text.replace(/\/\*[\s\S]*?\*\//g, "").replace(/(^|\s)\/\/.*$/gm, "$1");
const between = (text: string, from: string, to?: string) => {
  const start = text.indexOf(from);
  expect(start, `missing ${from}`).toBeGreaterThanOrEqual(0);
  const end = to ? text.indexOf(to, start) : -1;
  return text.slice(start, end > start ? end : undefined);
};
function csharp(folder: string): string[] {
  const found: string[] = [];
  for (const name of readdirSync(folder)) {
    const path = join(folder, name);
    if (statSync(path).isDirectory()) {
      if (!["bin", "obj"].includes(name)) found.push(...csharp(path));
    } else if (name.endsWith(".cs")) found.push(path);
  }
  return found;
}

const TOKEN = "plugin/Security/McpAuthToken.cs";
const SOCKET = "plugin/Core/SocketService.cs";
const ACTIONS = "plugin/Utils/ActionLogger.cs";

describe("0022: the token file", () => {
  it("is 32 bytes from a CSPRNG, issued at each listener start", () => {
    const token = code(read(TOKEN));
    expect(token).toMatch(/new byte\[TokenBytes\]/);
    expect(token).toMatch(/const int TokenBytes = 32;/);
    expect(token).toMatch(/RandomNumberGenerator\.Create\(\)[\s\S]*?\.GetBytes\(bytes\)/);
    expect(token).not.toMatch(/new Random\(|Guid\.NewGuid\(\)\.ToString\(\)\s*;/);
  });

  it("lives in %LOCALAPPDATA%\\Cemengal\\RevitMCP\\token, never %TEMP%, CemTempFolder or ACC", () => {
    const token = code(read(TOKEN));
    expect(token).toMatch(
      /Path\.Combine\(\s*Environment\.GetFolderPath\(\s*Environment\.SpecialFolder\.LocalApplicationData\s*\),\s*"Cemengal",\s*McpLocalFiles\.App\s*\)/
    );
    expect(token).toMatch(/Path\.Combine\(\s*Folder,\s*"token"\s*\)/);
    expect(token).not.toMatch(/GetTempPath|CemTempFolder|ACCDocs/);
  });

  it("the folder and the file allow the user and SYSTEM only, with inheritance off", () => {
    const token = code(read(TOKEN));
    // Two protected security descriptors (folder, file), with nothing inherited.
    expect(token.match(/SetAccessRuleProtection\(\s*true,\s*false\s*\)/g)).toHaveLength(2);
    // The only identities granted: the current user's SID and LocalSystem.
    expect(between(token, "private static SecurityIdentifier[] AllowedAccounts(", "[DllImport")).toMatch(
      /return new\[\] \{ user\.User, new SecurityIdentifier\(WellKnownSidType\.LocalSystemSid, null\) \};/
    );
    const granted = [...token.matchAll(/AddAccessRule\(new FileSystemAccessRule\((\w+),/g)].map((m) => m[1]);
    expect(granted).toEqual(["sid", "sid"]);
    expect(token.match(/foreach \(var sid in AllowedAccounts\(\)\)/g)).toHaveLength(2);
    expect(token).not.toMatch(/NTAccount|"Everyone"|"Users"|BuiltinUsersSid|WorldSid|AuthenticatedUserSid/);
    expect(token).toMatch(/new DirectoryInfo\(\s*folder\s*\)\.SetAccessControl\(/);
    expect(token).toMatch(/new FileInfo\(\s*temp\s*\)\.SetAccessControl\(\s*ProtectedFile\(\)\s*\)/);
  });

  it("is written atomically: a new temp file, its ACL before its content, then one replacing move", () => {
    const write = between(code(read(TOKEN)), "private static void Write(", "private static ");
    expect(write).toMatch(
      /new FileStream\(\s*temp,\s*FileMode\.CreateNew,[\s\S]*?new FileInfo\(\s*temp\s*\)\.SetAccessControl\([\s\S]*?new FileStream\(\s*temp,\s*FileMode\.Open,[\s\S]*?stream\.Write\([\s\S]*?ReplaceWithRetry\(\s*temp,\s*file\s*\)/
    );
    // Only Issue writes, and only to the token folder; Write takes the folder so the runtime test can use a scratch one.
    expect(between(code(read(TOKEN)), "public static void Issue(", "public static void Revoke(")).toMatch(
      /Write\(Folder, hex\.ToString\(\)\);/
    );
    expect(between(code(read(TOKEN)), "private static void Write(", "private static ")).toMatch(
      /string file = Path\.Combine\(folder, "token"\);/
    );
  });

  it("a reader holding the file delays the replacing move, within a bound; it never makes the write fail", () => {
    // MoveFileEx(REPLACE_EXISTING) fails with ERROR_ACCESS_DENIED or ERROR_SHARING_VIOLATION while another handle (the
    // TS client's read, an antivirus scan) has the token open. Proven at runtime in tests/plugin/runtime.test.ts.
    const replace = between(code(read(TOKEN)), "private static void ReplaceWithRetry(", "private static ");
    // A POSIX-semantics rename first (it replaces a file a delete-sharing reader holds), MoveFileEx where unsupported.
    expect(replace).toMatch(/ReplacePosix\(temp, file\)[\s\S]*?if \(error == PosixUnsupported\)\s*error = MoveFileEx\(/);
    expect(between(code(read(TOKEN)), "private static int ReplacePosix(", "private static ")).toMatch(
      /FileRenameFlagReplaceIfExists \| FileRenameFlagPosixSemantics[\s\S]*?SetFileInformationByHandle\(handle, FileRenameInfoEx,/
    );
    expect(replace).toMatch(/MoveFileEx\(\s*temp,\s*file,\s*MoveFileReplaceExisting \| MoveFileWriteThrough\s*\)/);
    expect(replace).toMatch(/error != ErrorAccessDenied && error != ErrorSharingViolation/);
    expect(replace).toMatch(/attempt >= ReplaceBackoffMs\.Length/);
    expect(replace).toMatch(/throw new IOException\(/);
    expect(code(read(TOKEN))).toMatch(/const int ErrorAccessDenied = 5;[\s\S]*?const int ErrorSharingViolation = 32;/);
  });

  it("never through a junction or link: the folders and the file are checked before and after the write", () => {
    const write = between(code(read(TOKEN)), "private static void Write(", "private static ");
    expect(write).toMatch(
      /RefuseReparsePoint\(\s*parent,\s*folder,\s*file\s*\)[\s\S]*?Directory\.CreateDirectory\(\s*folder\s*\)[\s\S]*?RefuseReparsePoint\(\s*parent,\s*folder,\s*file\s*\)[\s\S]*?ReplaceWithRetry[\s\S]*?RefuseReparsePoint\(\s*parent,\s*folder,\s*file\s*\)/
    );
    const local = code(read("plugin/Utils/McpLocalFiles.cs"));
    expect(local).toMatch(/FileAttributes\.ReparsePoint/);
    expect(local).toMatch(/throw new CemTempFolderUnsafeException\(path\)/);
  });

  it("a token folder another account owns (and could re-ACL) is refused", () => {
    const write = between(code(read(TOKEN)), "private static void Write(", "private static ");
    expect(write).toMatch(/RefuseForeignOwner\(folder\);\s*new DirectoryInfo\(folder\)\.SetAccessControl\(/);
    const owner = between(code(read(TOKEN)), "private static void RefuseForeignOwner(", "private static ");
    expect(owner).toMatch(/GetOwner\(typeof\(SecurityIdentifier\)\)/);
    expect(owner).toMatch(/throw new UnauthorizedAccessException\(/);
  });

  it("the listener issues the token once the port is bound and before it accepts a client, and revokes it on stop", () => {
    const socket = code(read(SOCKET));
    expect(between(socket, "public void Start()", "public void Stop()")).toMatch(
      /_listener\.Start\(\)[\s\S]*?McpAuthToken\.Issue\(\)[\s\S]*?_listenerThread\.Start\(\)/
    );
    expect(between(socket, "public void Stop()", "private void ListenForClients")).toMatch(/McpAuthToken\.Revoke\(\)/);
  });

  it("Revit's closing stops the listener and revokes the token, whether or not the host calls OnShutdown", () => {
    // CEM_RibbonUI, which hosts the plugin, has no OnShutdown: the token file outlived Revit (live check, 2026-11-01).
    const socket = code(read(SOCKET));
    expect(between(socket, "public void Initialize(", "public void Start()")).toMatch(/HookShutdown\(uiApp\);/);
    expect(between(socket, "private void HookShutdown(", "private void UnhookShutdown(")).toMatch(
      /if \(_closingSource != null\) return;[\s\S]*?uiApp\.ApplicationClosing \+= OnApplicationClosing;[\s\S]*?_closingSource = uiApp;/
    );
    expect(between(socket, "private void OnApplicationClosing(", "private void ListenForClients")).toMatch(
      /OnApplicationClosing\(object sender, ApplicationClosingEventArgs e\)\s*=>\s*Stop\(\);/
    );
  });

  it("the closing handler is removed on Stop, from the instance it was added to, and Stop stays idempotent", () => {
    const socket = code(read(SOCKET));
    expect(between(socket, "public void Stop()", "private void HookShutdown(")).toMatch(
      /UnhookShutdown\(\);\s*if \(!_isRunning\) return;[\s\S]*?McpAuthToken\.Revoke\(\)/
    );
    expect(between(socket, "private void UnhookShutdown(", "private void OnApplicationClosing(")).toMatch(
      /var source = _closingSource;\s*if \(source == null\) return;\s*_closingSource = null;\s*try \{ source\.ApplicationClosing -= OnApplicationClosing; \}/
    );
    // The plugin's own OnShutdown path stays, and finds the listener already stopped.
    expect(code(read("plugin/Core/Application.cs"))).toMatch(
      /if \(SocketService\.HasInstance && SocketService\.Instance\.IsRunning\)\s*\{\s*SocketService\.Instance\.Stop\(\);/
    );
  });

  it("a token that cannot be written stops the listener and says so, instead of a server nobody can use", () => {
    const start = between(code(read(SOCKET)), "public void Start()", "public void Stop()");
    expect(start).toMatch(/catch \(Exception ex\)[\s\S]*?_listener\.Stop\(\)[\s\S]*?throw new InvalidOperationException\(/);
  });
});

describe("0022: the plugin refuses a request without the token", () => {
  it("checks the token before CommandExecutor, and strips it before the command and the action log see the params", () => {
    const process = between(code(read(SOCKET)), "private string ProcessJsonRPCRequest(", "private string CreateSuccessResponse(");
    expect(process).toMatch(
      /!McpAuthToken\.Matches\([\s\S]*?return CreateErrorResponse\([\s\S]*?\.Remove\(\s*McpAuthToken\.Param\s*\)[\s\S]*?_commandExecutor\.ExecuteCommand\(request\)/
    );
  });

  it("answers the SDK's Unauthorized code with no detail, and the request's id", () => {
    const process = between(code(read(SOCKET)), "private string ProcessJsonRPCRequest(", "private string CreateSuccessResponse(");
    expect(process).toMatch(/CreateErrorResponse\(\s*request\.Id,\s*JsonRPCErrorCodes\.Unauthorized,\s*"Unauthorized"\s*\)/);
  });

  it("compares in constant time: no early exit, no string equality", () => {
    const compare = between(code(read(TOKEN)), "private static bool FixedTimeEquals(", "private static ");
    expect(compare).toMatch(/for \(int i = 0; i < wanted\.Length; i\+\+\)[\s\S]*?diff \|=/);
    expect(compare).not.toMatch(/for \([\s\S]*return false;|\bbreak;|string\.Equals|==\s*(expected|wanted)\b|\.SequenceEqual\(/);
    expect(compare).toMatch(/return diff == 0;/);
  });

  it("refuses everything once the listener stopped (no token in memory)", () => {
    const token = code(read(TOKEN));
    expect(between(token, "public static void Revoke(", "public static ")).toMatch(/_current = null;/);
    expect(between(token, "private static bool FixedTimeEquals(", "private static ")).toMatch(
      /if \(candidate == null \|\| expected == null\) return false;/
    );
  });

  it("stays loopback-only (0021)", () => {
    expect(code(read(SOCKET))).toMatch(/BindAddress = IPAddress\.Loopback;/);
  });

  it("holds its port exclusively (SEC-5.2, CWE-605): set on each new listener, before it binds", () => {
    // Start makes a fresh listener every time (Stop drops it), so the ribbon's stop/start sets it again.
    const start = between(code(read(SOCKET)), "public void Start()", "public void Stop()");
    expect(start).toMatch(
      /_listener = new TcpListener\(BindAddress, _port\);\s*_listener\.ExclusiveAddressUse = true;\s*_listener\.Start\(\);/
    );
    expect(start.match(/_listener\.Start\(\)/g)).toHaveLength(1);
  });
});

describe("0022: the token never reaches a log or a trace", () => {
  it("no log, trace or exception message in the plugin or the command set names the token", () => {
    // The token's own variables, as a format argument or an interpolation; words in the message text are fine.
    const secret = String.raw`(_current|token|issued|hex|presented|candidate)\b`;
    const leaks = new RegExp(
      String.raw`((_logger|Logger|Trace|Debug|Console)\.\w+|new \w*Exception)\(([^;]*[,{])?\s*${secret}`
    );
    const offenders = [...csharp(join(repo, "plugin")), ...csharp(join(repo, "commandset"))]
      .filter((f) => leaks.test(code(readFileSync(f, "utf8"))))
      .map((f) => f.slice(repo.length + 1).split("\\").join("/"));
    expect(offenders).toEqual([]);
  });

  it("the socket no longer traces the raw request, which carries the token", () => {
    const socket = code(read(SOCKET));
    expect(socket).not.toMatch(/Trace\.WriteLine\([^;]*\{message\}/);
  });

  it("the rejection is logged with the method only", () => {
    const process = between(code(read(SOCKET)), "private string ProcessJsonRPCRequest(", "private string CreateSuccessResponse(");
    expect(process).toMatch(/_logger\.Warning\([^;]*Unauthorized[^;]*,\s*Shorten\(request\.Method\)\s*\)/i);
  });

  it("caller text cannot forge mcp-*.log lines: every format argument is escaped (CWE-117)", () => {
    // The format strings are the plugin's own; the arguments (a refused method, a command name, an exception message)
    // can carry CR/LF from a caller. LogText's own behaviour is checked at runtime in tests/plugin/runtime.test.ts.
    const logger = code(read("plugin/Utils/Logger.cs"));
    expect(logger).toMatch(/string\.Format\(message, LogText\.EscapeArgs\(args\)\)/);
    // So no logger call may build its message by interpolation, which would bypass the escaping.
    const interpolated = csharp(join(repo, "plugin"))
      .filter((f) => /(_logger|Logger)\.\w+\(\s*\$"/.test(code(readFileSync(f, "utf8"))))
      .map((f) => f.slice(repo.length + 1).split("\\").join("/"));
    expect(interpolated).toEqual([]);
  });

  it("the refused method is escaped before it is shortened, and its refusal line names nothing else", () => {
    const socket = code(read(SOCKET));
    expect(socket).toMatch(/private static string Shorten\(string method\)[\s\S]*?LogText\.Escape\(method\)/);
  });

  it("the action log drops an auth param, whatever reaches it", () => {
    const input = between(code(read(ACTIONS)), "private static JToken BuildInputToken(", "private static string Truncate(");
    expect(input).toMatch(/\.Remove\(\s*"auth"\s*\)/);
  });
});
