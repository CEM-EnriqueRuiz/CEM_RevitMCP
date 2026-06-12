import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({
  x: z.number().describe("X in mm"),
  y: z.number().describe("Y in mm"),
  z: z.number().describe("Z in mm"),
});

export function registerPlaceViewportTool(server: McpServer) {
  server.tool(
    "place_viewport",
    "Place one or more views as viewports on an existing sheet. With a single view you may pass an explicit center (mm, sheet coordinates); otherwise viewports are auto-arranged in a grid. Skips views that are already placed.",
    {
      sheetId: z.number().int().describe("Target sheet ElementId"),
      viewIds: z.array(z.number().int()).min(1).describe("Views to place"),
      center: point.optional().describe("Placement center in mm (only used for a single view)"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("place_viewport", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `place_viewport failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
