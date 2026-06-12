import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");
export function registerArchCreateStairsTool(server: McpServer) {
  server.tool("arch_create_stairs", "Create a straight-run stair between two levels. Provide the run start/end (mm) defining direction and length, plus width. Type and levels resolved by name.", { baseLevel: z.string().optional(), topLevel: z.string().optional(), runStart: point, runEnd: point, width: z.number().default(1000).describe("Run width mm"), typeName: z.string().optional() },
    async (args) => { const params = { baseLevel: args.baseLevel ?? "", topLevel: args.topLevel ?? "", runStart: args.runStart, runEnd: args.runEnd, width: args.width, typeName: args.typeName ?? "" };
      try { const r = await withRevitConnection((c) => c.sendCommand("arch_create_stairs", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "arch_create_stairs failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
