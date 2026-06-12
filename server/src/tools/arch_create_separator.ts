import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");
export function registerArchCreateSeparatorTool(server: McpServer) {
  server.tool("arch_create_separator", "Create room/area/space separation lines from a polyline (mm) in a view. kind: room|area|space.", { kind: z.enum(["room","area","space"]).default("room"), points: z.array(point).min(2).describe("Polyline points (mm)"), viewId: z.number().int().default(-1).describe("-1 = active view"), closed: z.boolean().default(true) },
    async (args) => { const params = { kind: args.kind, points: args.points, viewId: args.viewId, closed: args.closed };
      try { const r = await withRevitConnection((c) => c.sendCommand("arch_create_separator", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "arch_create_separator failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
