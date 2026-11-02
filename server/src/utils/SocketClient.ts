import * as net from "net";
import { readFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";

// CEM divergence from upstream (decision 0022, SEC-5): the socket runs any C# through send_code_to_revit, and other
// local accounts can reach 127.0.0.1. Each listener start in Revit writes a fresh token, readable only by the user and
// SYSTEM; every request carries it as this param, and the plugin refuses a request without it.
export const AUTH_PARAM = "auth";
// RevitMCPSDK's JsonRPCErrorCodes.Unauthorized: the plugin's answer to a missing or wrong token.
export const UNAUTHORIZED = -33200;

/** %LOCALAPPDATA%\Cemengal\RevitMCP\token, where the plugin writes the token (read at call time). */
export function defaultTokenPath(): string {
  const local = process.env.LOCALAPPDATA || join(homedir(), "AppData", "Local");
  return join(local, "Cemengal", "RevitMCP", "token");
}

// The last token read from each file; read again when Revit refuses it (the listener restarted).
const tokens = new Map<string, string>();

function readToken(path: string, fresh: boolean): string {
  const cached = tokens.get(path);
  if (cached && !fresh) return cached;
  let token = "";
  let reason = "it is empty";
  try {
    token = readFileSync(path, "utf8").trim();
  } catch (error) {
    reason = (error as NodeJS.ErrnoException).code ?? "unreadable";
  }
  if (!token) {
    tokens.delete(path);
    throw new Error(
      `Cannot read the Revit MCP token at ${path} (${reason}). Start the MCP service in Revit: it writes a new token ` +
        `each time it starts. The MCP server must run as the same Windows user as Revit; under another account (such ` +
        `as a sandbox) the file is unreadable by design and Revit refuses every call.`
    );
  }
  tokens.set(path, token);
  return token;
}

export class RevitClientConnection {
  host: string;
  port: number;
  tokenPath: string;
  socket: net.Socket;
  isConnected: boolean = false;
  responseCallbacks: Map<string, (response: string) => void> = new Map();
  buffer: string = "";

  // tokenPath: only tests point it elsewhere.
  constructor(host: string, port: number, tokenPath: string = defaultTokenPath()) {
    this.host = host;
    this.port = port;
    this.tokenPath = tokenPath;
    this.socket = new net.Socket();
    this.setupSocketListeners();
  }

  private setupSocketListeners(): void {
    this.socket.on("connect", () => {
      this.isConnected = true;
    });

    this.socket.on("data", (data) => {
      // 将接收到的数据添加到缓冲区
      const dataString = data.toString();
      this.buffer += dataString;

      // 尝试解析完整的JSON响应
      this.processBuffer();
    });

    this.socket.on("close", () => {
      this.isConnected = false;
    });

    this.socket.on("error", (error) => {
      console.error("RevitClientConnection error:", error);
      this.isConnected = false;
    });
  }

  private processBuffer(): void {
    try {
      // 尝试解析JSON
      const response = JSON.parse(this.buffer);
      // 如果成功解析，处理响应并清空缓冲区
      this.handleResponse(this.buffer);
      this.buffer = "";
    } catch (e) {
      // 如果解析失败，可能是因为数据不完整，继续等待更多数据
    }
  }

  public connect(): boolean {
    if (this.isConnected) {
      return true;
    }

    try {
      this.socket.connect(this.port, this.host);
      return true;
    } catch (error) {
      console.error("Failed to connect:", error);
      return false;
    }
  }

  public disconnect(): void {
    this.socket.end();
    this.isConnected = false;
  }

  private generateRequestId(): string {
    return Date.now().toString() + Math.random().toString().substring(2, 8);
  }

  private handleResponse(responseData: string): void {
    try {
      const response = JSON.parse(responseData);
      // 从响应中获取ID
      const requestId = response.id || "default";

      const callback = this.responseCallbacks.get(requestId);
      if (callback) {
        callback(responseData);
        this.responseCallbacks.delete(requestId);
      }
    } catch (error) {
      console.error("Error parsing response:", error);
    }
  }

  public async sendCommand(command: string, params: any = {}): Promise<any> {
    let response = await this.request(command, params, readToken(this.tokenPath, false));
    if (response.error?.code === UNAUTHORIZED) {
      // Revit restarted its listener, which wrote a new token: read the file again and retry once. A refused request
      // never ran, so sending it again is safe.
      response = await this.request(command, params, readToken(this.tokenPath, true));
      if (response.error?.code === UNAUTHORIZED) {
        throw new Error(
          `Revit refused the request: the token in ${this.tokenPath} does not match its MCP service. Restart the ` +
            `service in Revit, and run the MCP server as the same Windows user as Revit.`
        );
      }
    }
    if (response.error) {
      throw new Error(response.error.message || "Unknown error from Revit");
    }
    return response.result;
  }

  // One JSON-RPC round trip: resolves with the whole response, error or result.
  private request(command: string, params: any, token: string): Promise<any> {
    return new Promise((resolve, reject) => {
      try {
        if (!this.isConnected) {
          this.connect();
        }

        // 生成请求ID
        const requestId = this.generateRequestId();

        // 创建符合JSON-RPC标准的请求对象
        const commandObj = {
          jsonrpc: "2.0",
          method: command,
          params: { ...(params ?? {}), [AUTH_PARAM]: token },
          id: requestId,
        };

        // 存储回调函数
        this.responseCallbacks.set(requestId, (responseData) => {
          clearTimeout(timer);
          try {
            resolve(JSON.parse(responseData));
          } catch (error) {
            if (error instanceof Error) {
              reject(new Error(`Failed to parse response: ${error.message}`));
            } else {
              reject(new Error(`Failed to parse response: ${String(error)}`));
            }
          }
        });

        // 发送命令
        const commandString = JSON.stringify(commandObj);
        this.socket.write(commandString);

        // 设置超时
        const timer = setTimeout(() => {
          if (this.responseCallbacks.has(requestId)) {
            this.responseCallbacks.delete(requestId);
            reject(new Error(`Command timed out after 2 minutes: ${command}`));
          }
        }, 120000); // 2分钟超时
      } catch (error) {
        reject(error);
      }
    });
  }
}
