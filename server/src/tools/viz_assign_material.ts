import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerVizAssignMaterialTool(server: McpServer) {
  server.tool(
    "viz_assign_material",
    'Assign a material to elements. mode="parameter" (default) sets the element/type "Material" parameter; mode="paint" paints all faces of each element with the material. Material resolved by name or id. Per-element failures are reported in warnings, not fatal.',
    {
      material: z.string().describe("Material name or id to assign"),
      elementIds: z.array(z.number().int()).min(1).describe("Element ids to assign the material to"),
      mode: z.enum(["parameter", "paint"]).default("parameter").describe("Assignment mode"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("viz_assign_material", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `viz_assign_material failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
