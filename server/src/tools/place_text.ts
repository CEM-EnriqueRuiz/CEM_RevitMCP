import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({
  x: z.number().describe("X in mm"),
  y: z.number().describe("Y in mm"),
  z: z.number().describe("Z in mm").default(0),
});

export function registerPlaceTextTool(server: McpServer) {
  server.tool(
    "place_text",
    "Create a text note at a point (mm) in a view. Optional TextNoteType (by name/id) and wrap width in mm (0 = unwrapped). View resolved by id/name (-1/empty = active). Returns the text note id.",
    {
      viewId: z.string().optional().describe("View id or name; omit/-1 for active view"),
      text: z.string().describe("Text content"),
      position: point.describe("Placement point in the view (mm)"),
      textTypeName: z.string().optional().describe("TextNoteType name or id; omit for default"),
      width: z.number().nonnegative().default(0).describe("Wrap width in mm; 0 = unwrapped"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("place_text", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `place_text failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
