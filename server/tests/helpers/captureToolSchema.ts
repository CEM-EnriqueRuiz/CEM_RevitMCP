import { z, ZodRawShape } from "zod";

/**
 * Every tool file calls `server.tool(name, description, zodRawShape, handler)`.
 * This stub stands in for the real McpServer so a test can import the real
 * `register*` export from `server/src/tools/<name>.ts` and recover the exact
 * zod shape that ships in production, without opening a socket or requiring
 * the MCP SDK's server plumbing.
 */
export interface CapturedTool {
  name: string;
  description: string;
  schemaShape: ZodRawShape;
  handler: (...args: any[]) => any;
}

export function createCapturingServerStub() {
  const tools = new Map<string, CapturedTool>();

  const stub = {
    tool(name: string, description: string, schemaShape: ZodRawShape, handler: (...args: any[]) => any) {
      tools.set(name, { name, description, schemaShape, handler });
    },
  };

  return { stub, tools };
}

/**
 * Registers one tool file's export against the capturing stub and returns a
 * real `z.object(shape)` built from the captured raw shape, ready for
 * `.safeParse()` in a test.
 */
export async function captureToolSchema(
  registerFn: (server: any) => void,
  expectedName: string
): Promise<z.ZodObject<ZodRawShape>> {
  const { stub, tools } = createCapturingServerStub();
  registerFn(stub);
  const captured = tools.get(expectedName);
  if (!captured) {
    throw new Error(
      `Tool "${expectedName}" was not registered by the given register function. ` +
        `Registered names: ${[...tools.keys()].join(", ") || "(none)"}`
    );
  }
  return z.object(captured.schemaShape);
}
