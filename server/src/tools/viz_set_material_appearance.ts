import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerVizSetMaterialAppearanceTool(server: McpServer) {
  server.tool(
    "viz_set_material_appearance",
    "Edit an EXISTING material's shading graphics: color [r,g,b], transparency (0-100), shininess (0-128), smoothness (0-100), surface/cut fill patterns (by name/id), and appearance asset (by name). Material resolved by name or id. Only the provided fields are changed.",
    {
      material: z.string().describe("Material name or id to modify"),
      colorRgb: z.array(z.number().int().min(0).max(255)).length(3).optional().describe("Shading color [r,g,b]"),
      transparency: z.number().int().min(0).max(100).optional().describe("Transparency 0-100"),
      shininess: z.number().int().min(0).max(128).optional().describe("Shininess 0-128"),
      smoothness: z.number().int().min(0).max(100).optional().describe("Smoothness 0-100"),
      surfacePatternName: z.string().optional().describe("Surface foreground fill pattern name or id"),
      cutPatternName: z.string().optional().describe("Cut foreground fill pattern name or id"),
      appearanceAssetName: z.string().optional().describe("Existing appearance asset name to assign"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("viz_set_material_appearance", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `viz_set_material_appearance failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
