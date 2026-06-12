import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({
  x: z.number().describe("X in mm"),
  y: z.number().describe("Y in mm"),
  z: z.number().describe("Z in mm"),
});

export function registerCreateViewTool(server: McpServer) {
  server.tool(
    "create_view",
    "Create a view of any common kind: FloorPlan, CeilingPlan, AreaPlan, Section, Elevation, 3D, or Drafting. Level and view-family-type are resolved by name (or omitted for sensible defaults). Sections/elevations need start+end points (mm). Optional scale, name and view template.",
    {
      viewType: z
        .enum(["FloorPlan", "CeilingPlan", "AreaPlan", "Section", "Elevation", "3D", "Drafting"])
        .describe("Kind of view to create"),
      name: z.string().optional().describe("Optional view name"),
      level: z.string().optional().describe("Level name or id (plan views); omit for lowest level"),
      viewFamilyTypeName: z.string().optional().describe("Specific ViewFamilyType name (optional)"),
      scale: z.number().int().positive().optional().describe("Scale denominator, e.g. 100 for 1:100"),
      templateName: z.string().optional().describe("View template name or id to apply (optional)"),
      start: point.optional().describe("Section/elevation cut line start (mm)"),
      end: point.optional().describe("Section/elevation cut line end (mm)"),
      depth: z.number().optional().describe("Section view depth in mm (default 5000)"),
      height: z.number().optional().describe("Section crop height in mm (default 4000)"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_view", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_view failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
