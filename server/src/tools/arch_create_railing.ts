import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");
export function registerArchCreateRailingTool(server: McpServer) {
  server.tool("arch_create_railing", "Create railings on an existing stairs/ramp host. position: Treads or Stringer. Railing type resolved by name.", { hostId: z.number().int().describe("Stairs/ramp host id"), typeName: z.string().optional(), position: z.enum(["Treads","Stringer"]).default("Treads") },
    async (args) => { const params = { hostId: args.hostId, typeName: args.typeName ?? "", position: args.position };
      try { const r = await withRevitConnection((c) => c.sendCommand("arch_create_railing", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "arch_create_railing failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
