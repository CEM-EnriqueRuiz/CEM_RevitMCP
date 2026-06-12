import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");
export function registerArchCreateCurtainWallTool(server: McpServer) {
  server.tool("arch_create_curtain_wall", "Create a curtain wall along a line (mm). Uses a curtain wall type (resolved by name, or the first curtain type) and a level.", { start: point, end: point, height: z.number().default(3000).describe("Height in mm"), typeName: z.string().optional(), level: z.string().optional() },
    async (args) => { const params = { start: args.start, end: args.end, height: args.height, typeName: args.typeName ?? "", level: args.level ?? "" };
      try { const r = await withRevitConnection((c) => c.sendCommand("arch_create_curtain_wall", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "arch_create_curtain_wall failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
