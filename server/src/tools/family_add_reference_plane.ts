import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerFamilyAddReferencePlaneTool(server: McpServer) {
  server.tool(
    "family_add_reference_plane",
    "FAMILY EDITOR ONLY (fails clearly in a project): add a named reference plane defined by bubbleEnd, freeEnd and a cutVectorPoint (all mm). Reference planes are the dimensioning/constraint backbone for parametric families.",
    {
      bubbleEnd: point.describe("Bubble end (mm)"),
      freeEnd: point.describe("Free end (mm)"),
      cutVectorPoint: point.describe("Third point giving the plane's cut vector (mm)"),
      name: z.string().optional().describe("Optional reference plane name"),
    },
    async (args) => {
      const params = { ...args, name: args.name ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_add_reference_plane", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_add_reference_plane failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
