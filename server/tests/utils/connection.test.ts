// Verifies: server/src/utils/ConnectionManager.ts targets the IPv4 loopback listener.
// Traceability: docs/decisions/0021-loopback-only-socket.md. The Revit plugin binds
// SocketService.BindAddress = IPAddress.Loopback (127.0.0.1 only), so the client must connect
// to 127.0.0.1 explicitly; 'localhost' may resolve to ::1 first and miss the listener.
import * as net from "net";
import { afterEach, describe, expect, it } from "vitest";
import { REVIT_HOST, REVIT_PORT, withRevitConnection } from "../../src/utils/ConnectionManager.js";

let server: net.Server | undefined;

afterEach(async () => {
  await new Promise<void>((resolve) => (server ? server.close(() => resolve()) : resolve()));
  server = undefined;
});

// A stand-in for the Revit plugin: answers each JSON-RPC request with the method name and the
// address the request arrived on.
function listenFakeRevit(host: string, port: number): Promise<net.Server | null> {
  return new Promise((resolve, reject) => {
    const s = net.createServer((socket) => {
      socket.on("data", (data) => {
        const request = JSON.parse(data.toString());
        socket.write(
          JSON.stringify({
            jsonrpc: "2.0",
            id: request.id,
            result: { method: request.method, localAddress: socket.localAddress },
          })
        );
      });
    });
    s.once("error", (err: NodeJS.ErrnoException) =>
      err.code === "EADDRINUSE" ? resolve(null) : reject(err)
    );
    s.listen(port, host, () => resolve(s));
  });
}

describe("Revit connection target", () => {
  it("pins the client to IPv4 loopback 127.0.0.1:8080, not 'localhost'", () => {
    expect(REVIT_HOST).toBe("127.0.0.1");
    expect(REVIT_PORT).toBe(8080);
  });

  it("reaches a listener bound only to 127.0.0.1", async (ctx) => {
    const s = await listenFakeRevit("127.0.0.1", REVIT_PORT);
    if (!s) {
      ctx.skip(); // port 8080 busy (e.g. the real plugin is running); nothing to assert against
      return;
    }
    server = s;

    const result = await withRevitConnection((client) => client.sendCommand("say_hello", {}));

    expect(result).toEqual({ method: "say_hello", localAddress: "127.0.0.1" });
  });
});
