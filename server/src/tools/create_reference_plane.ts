import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

// All coordinates are in millimeters (mm); conversion to Revit internal units
// happens inside the C# handler.
const point = z.object({
  x: z.number().describe("X coordinate in mm"),
  y: z.number().describe("Y coordinate in mm"),
  z: z.number().describe("Z coordinate in mm"),
});

const referencePlane = z
  .object({
    creationMethod: z
      .enum(["ByLine", "ByNormal", "ByPoints"])
      .describe(
        "How to define the plane: 'ByLine' (bubbleEnd+freeEnd), 'ByNormal' (origin+normal+length), or 'ByPoints' (bubbleEnd+freeEnd+thirdPoint)"
      ),
    name: z.string().optional().describe("Optional name for the reference plane"),
    bubbleEnd: point.optional().describe("Bubble end point (ByLine / ByPoints)"),
    freeEnd: point.optional().describe("Free end point (ByLine / ByPoints)"),
    thirdPoint: point.optional().describe("Third point defining the plane (ByPoints)"),
    origin: point.optional().describe("Origin point (ByNormal)"),
    normal: point.optional().describe("Normal vector (ByNormal)"),
    length: z.number().optional().describe("Plane length in mm (ByNormal)"),
    viewId: z
      .number()
      .int()
      .optional()
      .describe("Target view ElementId; omit or -1 for the active view"),
  })
  .describe("A single reference plane definition");

export function registerCreateReferencePlaneTool(server: McpServer) {
  server.tool(
    "create_reference_plane",
    "Create one or more reference planes in Revit. Three creation methods adapt to whatever geometric data is available: ByLine (two points), ByNormal (origin + normal + length), or ByPoints (three points). All units in millimeters (mm). Returns the created element IDs.",
    {
      data: z.array(referencePlane).min(1).describe("List of reference planes to create"),
    },
    async (args, extra) => {
      const params = { data: args.data };
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_reference_plane", params);
        });

        return {
          content: [{ type: "text", text: JSON.stringify(response, null, 2) }],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `Create reference plane failed: ${
                error instanceof Error ? error.message : String(error)
              }`,
            },
          ],
        };
      }
    }
  );
}
