import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerPlaceImageTool(server: McpServer) {
  server.tool(
    "place_image",
    "Import an image file (.png/.jpg/.bmp/.tif by absolute path) and place it as an image instance in a view, centered at a point (mm) or at the view center when omitted. View resolved by id/name (-1/empty = active). Returns the image type and instance ids.",
    {
      path: z.string().describe("Absolute path to the image file"),
      viewId: z.string().optional().describe("View id or name; omit/-1 for active view"),
      center: point.optional().describe("Placement center (mm); omit for view center"),
    },
    async (args) => {
      const params = { ...args, viewId: args.viewId ?? "", center: args.center ?? null };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("place_image", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `place_image failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
