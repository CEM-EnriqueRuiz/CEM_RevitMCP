import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");
export function registerArchCreateAreaTool(server: McpServer) {
  server.tool("arch_create_area", "Place areas at points (mm) in an area plan view.", { areaViewId: z.number().int().describe("Area plan view id"), points: z.array(point).min(1).describe("Points (mm)") },
    async (args) => { const params = { areaViewId: args.areaViewId, points: args.points };
      try { const r = await withRevitConnection((c) => c.sendCommand("arch_create_area", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "arch_create_area failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
