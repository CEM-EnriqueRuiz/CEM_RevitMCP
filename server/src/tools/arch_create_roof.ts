import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");
export function registerArchCreateRoofTool(server: McpServer) {
  server.tool("arch_create_roof", "Create a footprint roof from a closed boundary of points (mm). Roof type and level resolved by name (or defaulted).", { boundary: z.array(point).min(3).describe("Closed boundary (mm)"), typeName: z.string().optional(), level: z.string().optional() },
    async (args) => { const params = { boundary: args.boundary, typeName: args.typeName ?? "", level: args.level ?? "" };
      try { const r = await withRevitConnection((c) => c.sendCommand("arch_create_roof", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "arch_create_roof failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
