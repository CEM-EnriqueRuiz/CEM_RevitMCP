import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerCreateModelLinesTool(server: McpServer) {
  server.tool(
    "create_model_lines",
    "Create 3D model lines from a polyline (points in mm). Each consecutive pair becomes one model line. A sketch plane through the first point is created with the chosen normal (XY/XZ/YZ). Set closed=true to connect the last point back to the first.",
    {
      points: z.array(point).min(2).describe("Polyline vertices (mm)"),
      closed: z.boolean().default(false).describe("Close the loop"),
      plane: z.enum(["XY", "XZ", "YZ"]).default("XY").describe("Work-plane normal direction"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_model_lines", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_model_lines failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
