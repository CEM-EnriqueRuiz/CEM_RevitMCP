import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");
export function registerArchCreateAreaPlanTool(server: McpServer) {
  server.tool("arch_create_area_plan", "Create an area plan view for an area scheme and level (resolved by name).", { level: z.string().optional(), schemeName: z.string().optional().describe("Area scheme name"), name: z.string().optional().describe("View name") },
    async (args) => { const params = { level: args.level ?? "", schemeName: args.schemeName ?? "", name: args.name ?? "" };
      try { const r = await withRevitConnection((c) => c.sendCommand("arch_create_area_plan", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "arch_create_area_plan failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
