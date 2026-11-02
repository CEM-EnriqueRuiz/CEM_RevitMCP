// Runtime checks of plugin code that never touches Revit (decision 0022): the built net48 CEM_IAModeler.dll is loaded
// into Windows PowerShell by tests/fixtures/plugin-runtime.ps1, unlike the Revit-bound C# that is only pinned in its
// source (0016). It checks:
// 1. LogText escapes what a caller controls (CR, LF, the other C0 and C1 controls, U+2028/U+2029) before mcp-*.log;
// 2. McpAuthToken.Write survives readers holding the file: 200 writes while an antivirus-like .NET reader and the TS
//    client's readFileSync loop keep opening it all succeed, and every read is a whole token.
// The DLL must be built from the current source (plugin Debug R24); a stale one fails, a missing one skips.
import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdtempSync, rmSync, statSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { afterAll, beforeAll, describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const repo = resolve(here, "..", "..", "..");
const dll = join(repo, "plugin", "bin", "Debug", "2024", "CEM_IAModeler.dll");
const sources = ["plugin/Security/McpAuthToken.cs", "plugin/Utils/LogText.cs", "plugin/Utils/Logger.cs", "plugin/Utils/McpLocalFiles.cs"];
const available = process.platform === "win32" && existsSync(dll);

type Runtime = {
  logText: boolean;
  escaped?: number[];
  idempotent?: boolean;
  plain?: number[];
  plainIn?: number[];
  nullEscaped?: string | null;
  formatted?: string;
  writeSeam: boolean;
  issues: number;
  failures: number;
  firstError: string | null;
  dotnetReads: number;
  lastMatches?: boolean;
  protected?: boolean;
  identities?: string[];
  entries: string[];
  user: string;
  ms: number;
};
type Reader = { reads: number; partial: number; enoentAfterFirst: number; errors: Record<string, number> };

describe.skipIf(!available)("plugin runtime (built CEM_IAModeler.dll, no Revit)", () => {
  let scratch = "";
  let runtime: Runtime;
  let reader: Reader;

  beforeAll(async () => {
    scratch = mkdtempSync(join(tmpdir(), "cem-revitmcp-runtime-"));
    const folder = join(scratch, "RevitMCP");
    const stop = join(scratch, "stop");
    const nodeReader = spawn(process.execPath, [join(here, "..", "fixtures", "token-reader.cjs"), join(folder, "token"), stop]);
    let readerOut = "";
    nodeReader.stdout.on("data", (d) => (readerOut += d));
    const readerDone = new Promise((done) => nodeReader.on("close", done));
    const ps = spawnSync(
      "powershell.exe",
      ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", join(here, "..", "fixtures", "plugin-runtime.ps1"),
       "-Dll", dll, "-Folder", folder, "-StopFile", stop, "-Issues", "200"],
      { encoding: "utf8", timeout: 170_000 }
    );
    await readerDone;
    const json = ps.stdout.trim().split(/\r?\n/).pop() ?? "";
    expect(json, ps.stderr).toMatch(/^\{/);
    runtime = JSON.parse(json);
    reader = JSON.parse(readerOut.trim());
  }, 180_000);

  afterAll(() => {
    if (scratch) rmSync(scratch, { recursive: true, force: true });
  });

  it("the DLL is built from the current source (rebuild plugin Debug R24 otherwise)", () => {
    const built = statSync(dll).mtimeMs;
    const stale = sources.filter((f) => existsSync(join(repo, f)) && statSync(join(repo, f)).mtimeMs > built);
    expect(stale).toEqual([]);
  });

  it("LogText leaves no line break or control character in caller text, and is idempotent", () => {
    expect(runtime.logText).toBe(true);
    const out = String.fromCharCode(...(runtime.escaped ?? []));
    // Built from char codes: U+2028/U+2029 are line terminators, so they cannot appear raw in a regex literal.
    const controls = new RegExp(`[\\x00-\\x1f\\x7f-\\x9f${String.fromCharCode(0x2028, 0x2029)}]`);
    expect(out).not.toMatch(controls);
    expect(out).toBe(String.raw`a\r\nb\tc\u0000\u001B\u007F\u0085\u009B\u2028\u2029d`);
    expect(runtime.idempotent).toBe(true);
  });

  it("LogText keeps ordinary text, accents included, and null as (null)", () => {
    expect(runtime.plain).toEqual(runtime.plainIn);
    expect(runtime.nullEscaped).toBe("(null)");
  });

  it("LogText.EscapeArgs escapes strings and keeps numbers formattable", () => {
    expect(runtime.formatted).toBe("x\\ny|1.5");
  });

  it("200 token writes all succeed while readers keep opening the file", () => {
    expect(runtime.writeSeam).toBe(true);
    expect({ failures: runtime.failures, firstError: runtime.firstError }).toEqual({ failures: 0, firstError: null });
    expect(runtime.lastMatches).toBe(true);
    // The readers really were reading during the writes (the .NET one about every 30 ms at the default timer resolution).
    expect(runtime.dotnetReads).toBeGreaterThan(10);
    expect(reader.reads).toBeGreaterThan(50);
  });

  it("every read is a whole token, the file never vanishes once written, and the ACL and folder stay clean", () => {
    expect({ partial: reader.partial, enoentAfterFirst: reader.enoentAfterFirst }).toEqual({ partial: 0, enoentAfterFirst: 0 });
    expect(runtime.protected).toBe(true);
    expect([...(runtime.identities ?? [])].sort()).toEqual([runtime.user, "S-1-5-18"].sort());
    expect(runtime.entries).toEqual(["token"]);
  });
});
