import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilyAddParameterTool(server: McpServer) {
  server.tool(
    "family_add_parameter",
    "Add a parameter to the family currently open in the Family Editor (an .rfa). Fails clearly if the active document is a project, not a family. Optional formula. Works across Revit versions.",
    {
      name: z.string().describe("Parameter name"),
      dataType: z
        .enum(["Text", "Integer", "Number", "Length", "Area", "Volume", "Angle", "YesNo", "Material"])
        .default("Text")
        .describe("Parameter data type"),
      group: z
        .enum(["Data", "Text", "IdentityData", "Dimensions", "Construction", "General", "Materials"])
        .default("Data")
        .describe("UI group in the properties palette"),
      isInstance: z.boolean().default(false).describe("Instance parameter (else type)"),
      formula: z.string().optional().describe("Optional formula to set on the new parameter"),
    },
    async (args) => {
      const params = { name: args.name, dataType: args.dataType, group: args.group, isInstance: args.isInstance, formula: args.formula ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_add_parameter", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_add_parameter failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
