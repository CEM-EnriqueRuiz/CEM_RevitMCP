// Verifies decision 0022 (docs/decisions/0022-local-shared-secret-handshake.md, SEC-5) on the client side: the TS
// server reads the token the Revit plugin writes at each listener start (%LOCALAPPDATA%\Cemengal\RevitMCP\token) and
// sends it as the `auth` param of every request; on an Unauthorized answer (Revit restarted, a new token) it reads the
// file again and retries once. The token never appears in an error or on the console. The plugin side is pinned in
// guards/socket-auth-pins.test.ts (its C# runs only inside Revit, 0016).
import * as net from "net";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AUTH_PARAM, RevitClientConnection, UNAUTHORIZED, defaultTokenPath } from "../../src/utils/SocketClient.js";

const TOKEN_A = "a".repeat(64);
const TOKEN_B = "b".repeat(64);

let dir: string;
let tokenFile: string;
let server: net.Server | undefined;
let client: RevitClientConnection | undefined;

beforeEach(() => {
  dir = mkdtempSync(join(tmpdir(), "revitmcp-auth-"));
  tokenFile = join(dir, "token");
});

afterEach(async () => {
  client?.disconnect();
  client = undefined;
  await new Promise<void>((resolve) => (server ? server.close(() => resolve()) : resolve()));
  server = undefined;
  rmSync(dir, { recursive: true, force: true });
  vi.restoreAllMocks();
  vi.unstubAllEnvs();
  vi.useRealTimers();
});

// A stand-in for the plugin's SocketService: answers Unauthorized, with no detail, unless `auth` is the expected
// token, and records what it received. `answer: false` never answers (the timeout path).
type Fake = { requests: any[]; expected: { token: string }; received: Promise<void> };
async function fakeRevit(token: string, answer = true): Promise<{ fake: Fake; port: number }> {
  const fake: Fake = { requests: [], expected: { token }, received: Promise.resolve() };
  let markReceived!: () => void;
  fake.received = new Promise((resolve) => (markReceived = resolve));
  server = net.createServer((socket) => {
    socket.on("data", (data) => {
      const request = JSON.parse(data.toString());
      fake.requests.push(request);
      markReceived();
      if (!answer) return;
      const ok = request.params?.[AUTH_PARAM] === fake.expected.token;
      socket.write(
        JSON.stringify(
          ok
            ? { jsonrpc: "2.0", id: request.id, result: { method: request.method } }
            : { jsonrpc: "2.0", id: request.id, error: { code: UNAUTHORIZED, message: "Unauthorized" } }
        )
      );
    });
  });
  await new Promise<void>((resolve) => server!.listen(0, "127.0.0.1", () => resolve()));
  return { fake, port: (server!.address() as net.AddressInfo).port };
}

async function connect(port: number): Promise<RevitClientConnection> {
  client = new RevitClientConnection("127.0.0.1", port, tokenFile);
  await new Promise<void>((resolve, reject) => {
    client!.socket.once("connect", () => resolve());
    client!.socket.once("error", reject);
    client!.connect();
  });
  return client;
}

describe("0022: the client authenticates every request", () => {
  it("reads the token file and sends it as the auth param, with the tool's params unchanged", async () => {
    writeFileSync(tokenFile, TOKEN_A);
    const { fake, port } = await fakeRevit(TOKEN_A);
    const c = await connect(port);

    await expect(c.sendCommand("say_hello", { x: 1 })).resolves.toEqual({ method: "say_hello" });
    await expect(c.sendCommand("say_again", {})).resolves.toEqual({ method: "say_again" });

    expect(fake.requests.map((r) => r.params)).toEqual([
      { x: 1, [AUTH_PARAM]: TOKEN_A },
      { [AUTH_PARAM]: TOKEN_A },
    ]);
  });

  it("ignores the line break an editor may leave at the end of the file", async () => {
    writeFileSync(tokenFile, TOKEN_A + "\r\n");
    const { fake, port } = await fakeRevit(TOKEN_A);
    await expect((await connect(port)).sendCommand("say_hello")).resolves.toEqual({ method: "say_hello" });
    expect(fake.requests[0].params[AUTH_PARAM]).toBe(TOKEN_A);
  });

  it("after Revit restarts (a new token), reads the file again and retries once", async () => {
    writeFileSync(tokenFile, TOKEN_A);
    const { fake, port } = await fakeRevit(TOKEN_A);
    const c = await connect(port);
    await c.sendCommand("first", {}); // the client now holds TOKEN_A

    fake.expected.token = TOKEN_B; // the listener restarted and wrote a new token
    writeFileSync(tokenFile, TOKEN_B);
    await expect(c.sendCommand("second", { y: 2 })).resolves.toEqual({ method: "second" });

    expect(fake.requests.map((r) => [r.method, r.params[AUTH_PARAM]])).toEqual([
      ["first", TOKEN_A],
      ["second", TOKEN_A],
      ["second", TOKEN_B],
    ]);
  });

  it("a wrong token is refused after one retry, and the error names the file, never the token", async () => {
    writeFileSync(tokenFile, TOKEN_A);
    const { fake, port } = await fakeRevit(TOKEN_B);
    const errors = vi.spyOn(console, "error").mockImplementation(() => {});

    const error: Error = await (await connect(port)).sendCommand("say_hello", {}).catch((e) => e);

    expect(fake.requests).toHaveLength(2);
    expect(error.message).toMatch(/refused/i);
    expect(error.message).toContain(tokenFile);
    expect(error.message).not.toContain(TOKEN_A);
    expect(error.message).not.toContain(TOKEN_B);
    for (const call of errors.mock.calls) expect(JSON.stringify(call)).not.toContain(TOKEN_A);
  });

  it("without a readable token file nothing is sent, and the error says why (Revit's service, the same Windows user)", async () => {
    const { fake, port } = await fakeRevit(TOKEN_A);

    const error: Error = await (await connect(port)).sendCommand("say_hello", {}).catch((e) => e);

    expect(fake.requests).toHaveLength(0);
    expect(error.message).toContain(tokenFile);
    expect(error.message).toMatch(/start the MCP service in Revit/i);
    expect(error.message).toMatch(/same Windows user/i);
    expect(error.message).toMatch(/sandbox/i);
  });

  it("an empty token file is refused the same way, before anything is sent", async () => {
    writeFileSync(tokenFile, "");
    const { fake, port } = await fakeRevit(TOKEN_A);
    const error: Error = await (await connect(port)).sendCommand("say_hello", {}).catch((e) => e);
    expect(fake.requests).toHaveLength(0);
    expect(error.message).toContain(tokenFile);
  });

  it("the 120 s timeout still applies, and a timed-out request is not retried", async () => {
    writeFileSync(tokenFile, TOKEN_A);
    const { fake, port } = await fakeRevit(TOKEN_A, false);
    const c = await connect(port);
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });

    const pending = c.sendCommand("slow", {}).catch((e) => e);
    await fake.received;
    await vi.advanceTimersByTimeAsync(120000);

    expect((await pending).message).toBe("Command timed out after 2 minutes: slow");
    expect(fake.requests).toHaveLength(1);
  });
});

describe("0022: where the token lives", () => {
  it("is %LOCALAPPDATA%\\Cemengal\\RevitMCP\\token, never %TEMP% or ACC", () => {
    vi.stubEnv("LOCALAPPDATA", "C:\\Users\\someone\\AppData\\Local");
    expect(defaultTokenPath()).toBe(join("C:\\Users\\someone\\AppData\\Local", "Cemengal", "RevitMCP", "token"));
  });

  it("the Unauthorized code is the SDK's JsonRPCErrorCodes.Unauthorized", () => {
    expect(UNAUTHORIZED).toBe(-33200);
    expect(AUTH_PARAM).toBe("auth");
  });
});
