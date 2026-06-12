import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({
  x: z.number().describe("X in mm"),
  y: z.number().describe("Y in mm"),
  z: z.number().describe("Z in mm"),
});

export function registerTransformElementsTool(server: McpServer) {
  server.tool(
    "transform_elements",
    "Move, rotate, mirror, copy, or array a batch of elements. All distances in mm, angles in degrees. 'copy' and 'array' return the new element ids; 'array' makes `count` copies each offset by `translation`.",
    {
      elementIds: z.array(z.number().int()).min(1).describe("Elements to transform"),
      operation: z
        .enum(["move", "rotate", "mirror", "copy", "array"])
        .describe("Transform operation"),
      translation: point.optional().describe("Vector in mm for move/copy/array step"),
      axisPoint: point.optional().describe("Rotation axis origin (rotate); default origin"),
      axisDirection: point.optional().describe("Rotation axis direction (rotate); default Z up"),
      angle: z.number().optional().describe("Rotation angle in degrees (rotate)"),
      mirrorPlaneOrigin: point.optional().describe("Mirror plane origin in mm"),
      mirrorPlaneNormal: point.optional().describe("Mirror plane normal; default X"),
      copy: z.boolean().default(true).describe("Keep originals (mirror/copy)"),
      count: z.number().int().positive().default(1).describe("Number of copies for array"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("transform_elements", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `transform_elements failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
