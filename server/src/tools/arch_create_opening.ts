import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

export function registerArchCreateOpeningTool(server: McpServer) {
  server.tool(
    "arch_create_opening",
    "Create an opening in a host element. If the host is a wall, a rectangular opening is made between the two extreme corners of the boundary. For floors/roofs/ceilings, a vertical shaft-style opening is cut using the boundary profile (mm).",
    {
      hostId: z.number().int().describe("Host element id (wall, floor, roof, ceiling)"),
      boundary: z.array(point).min(2).describe("Boundary points in mm (2 corners for a wall; a closed loop for a shaft)"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("arch_create_opening", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `arch_create_opening failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
